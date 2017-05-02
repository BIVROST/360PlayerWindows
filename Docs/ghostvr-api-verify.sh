#!/usr/bin/env bash

INSTALLATION_ID="$(uuidgen)"
ACCESS_TOKEN="$(uuidgen)"
GHOSTVR="http://dev.ghostvr.io/api/v1/"

xcurl() {
	(>&2 echo "DEBUG CURL:" "$@")
	command ssh odyn curl "$@" | tee /dev/stderr
	echo "" >/dev/stderr
}

browser() {
	echo "will open: $1"
	if hash open 2>/dev/null; then
		open "$1"
	elif hash cygstart 2>/dev/null; then
		cygstart "$1"
	else
		echo "Uruchom w przeglądarce: $1"
	fi
}


if ! hash jq 2>/dev/null; then
	echo "Wymaga narzedzia JQ: https://stedolan.github.io/jq/manual/"
	exit 1;
fi

echo "PREMATURE VERIFY TOKEN"
curl -s "${GHOSTVR}verify_player_token?access_token=${ACCESS_TOKEN}" | jq .response.verification_status


echo ""
echo "AUTHORIZE_PLAYER"

AUTH_URI="${GHOSTVR}authorize_player?access_token=${ACCESS_TOKEN}&installation_id=${INSTALLATION_ID}&player%5Bname%5D=Bivrost%20360%20Player&player%5Bversion%5D=1.2.3"

# fix
AUTH_URI="http://bivrost.dev.ghostvr.io/ui/authorize_player?access_token=${ACCESS_TOKEN}&installation_id=${INSTALLATION_ID}&player%5Bname%5D=Bivrost%20360%20Player&player%5Bversion%5D=1.2.3"
browser "$AUTH_URI"

sleep 3

echo ""
echo "VERIFY_TOKEN"
STATUS="pending"
while true; do
	STATUS=$(curl -s "${GHOSTVR}verify_player_token?access_token=${ACCESS_TOKEN}" | jq -r .data.verification_status | tr -d '\n\r')
	sleep 1
	echo "  verification_status:$STATUS $(xxd <<< "$STATUS")"
	case "$STATUS" in
		"ok")
			break;
			;;
			
		"rejected")
			echo "TOKEN REJECTED";
			exit
			;;
	esac
done


echo ""
echo "SEND VIDEO SESSION"
SESSION=$(cat<<END
{
	"guid":"$(uuidgen)",
	"uri":"D:/Video/test.mp4",
	"sample_rate":10,
	"installation_id":"${INSTALLATION_ID}",
	"time_start":"2016-09-26T12:10:59+02:00",
	"time_end":"2016-09-26T12:11:03+02:00",
	"lookprovider":"bivrost:360player:Oculus",
	"history":"----------!F75!v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1v1"
}
END
)

FOLLOW_UP=$(curl -v "${GHOSTVR}video_session" -H "Authorization: Bearer $ACCESS_TOKEN" -H 'Content-Type: application/json; charset=UTF-8' --data "$SESSION" | jq -r .data.follow_up | tr -d '\n\r')
echo "follow_up=$FOLLOW_UP"
# fix
FOLLOW_UP="$FOLLOW_UP&access_token=$ACCESS_TOKEN"
browser "$FOLLOW_UP"

echo "...wait for discard (enter)..."
read __unused

	curl -s "${GHOSTVR}verify_player_token?access_token=${ACCESS_TOKEN}"

	echo ""
echo "DISCARD TOKEN/GET"
curl -s "${GHOSTVR}discard_player_token?access_token=${ACCESS_TOKEN}"

echo ""
echo "DISCARD TOKEN/POST"
curl -s "${GHOSTVR}discard_player_token" -d "access_token=${ACCESS_TOKEN}"

echo ""
echo "DISCARD TOKEN/HEADER"
curl -s "${GHOSTVR}discard_player_token" -H "Authorization: Bearer ${ACCESS_TOKEN}" 



echo ""
echo "VERIFY_TOKEN (x10)"
for i in {1..10}; do
	curl -s "${GHOSTVR}verify_player_token?access_token=${ACCESS_TOKEN}"
	sleep 1
done



