// PROPOSAL ONLY: not applied to GUIDNY/pr. Copy it there when the site owner approves.
// Target path in GUIDNY/pr: src/app/(shop)/game-checkout/route.ts
//
// The game links to /game-checkout?items=slug1,slug2 (checkout_url in
// store-game/products.config.json). This fills the visitor's normal cart with
// those products, using the same addToCartAction as the product page's buy
// button, and hands over to the existing /checkout (Pelecard). The game never
// sees card details.
import { NextResponse, type NextRequest } from "next/server";
import { db } from "@/lib/db";
import { getCart } from "@/lib/cart";
import { PUBLIC_PRODUCT_WHERE } from "@/lib/queries/products";
import { addToCartAction } from "@/actions/cart";

export const dynamic = "force-dynamic";

const SLUG = /^[a-z0-9-]{1,160}$/;
const MAX_ITEMS = 10;

export async function GET(req: NextRequest) {
  const params = req.nextUrl.searchParams;
  const slugs = [...new Set((params.get("items") ?? "").split(",").map((s) => s.trim()))]
    .filter((s) => SLUG.test(s))
    .slice(0, MAX_ITEMS);

  const products = slugs.length
    ? await db.product.findMany({
        where: { slug: { in: slugs }, ...PUBLIC_PRODUCT_WHERE },
        select: { id: true, stockStatus: true },
      })
    : [];

  // Opening the link twice must not double the quantities already in the cart.
  const inCart = new Set((await getCart()).items.map((i) => i.productId));
  let added = 0;
  for (const p of products) {
    if (p.stockStatus === "OUT_OF_STOCK" || inCart.has(p.id)) continue;
    try {
      await addToCartAction(p.id, 1);
      added++;
    } catch {
      // A product that became unavailable is skipped; the rest still check out.
    }
  }

  const to = new URL(added || inCart.size ? "/checkout" : "/cart", req.nextUrl.origin);
  for (const key of ["utm_source", "utm_medium", "utm_campaign"]) {
    const v = params.get(key);
    if (v) to.searchParams.set(key, v);
  }
  return NextResponse.redirect(to, 303);
}
