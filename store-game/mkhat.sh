# mkhat.sh <name> <prompt-file> : concept picture -> textured 3D model <name>-obj.glb (no rig)
N=$1; P=$(python3 -c "import json,sys;print(json.dumps(open('$2').read().strip()))")
R=$(curl -s -X POST https://api.meshy.ai/openapi/v1/text-to-image -H "Content-Type: application/json" -d "{\"ai_model\":\"gpt-image-2-5-flare\",\"aspect_ratio\":\"1:1\",\"prompt\":$P}")
IMG=$(echo "$R" | python3 -c "import sys,json;print(json.load(sys.stdin).get('result',''))"); [ -z "$IMG" ] && { echo "IMGFAIL $R"; exit 1; }
bash poll.sh text-to-image $IMG $N-img.json > /dev/null
python3 -c "import json,urllib.request;d=json.load(open('$N-img.json'));urllib.request.urlretrieve(d['image_urls'][0],'$N-concept.png')"
R=$(curl -s -X POST https://api.meshy.ai/openapi/v1/image-to-3d -H "Content-Type: application/json" -d "{\"input_task_id\":\"$IMG\",\"ai_model\":\"latest\",\"should_texture\":true,\"should_remesh\":true,\"target_polycount\":8000,\"topology\":\"triangle\",\"texture_resolution\":\"1k\",\"target_formats\":[\"glb\"]}")
T3=$(echo "$R" | python3 -c "import sys,json;print(json.load(sys.stdin).get('result',''))"); [ -z "$T3" ] && { echo "3DFAIL $R"; exit 1; }
bash poll.sh image-to-3d $T3 $N-3d.json > /dev/null
python3 dl3d.py $N
