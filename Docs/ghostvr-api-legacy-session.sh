#!/usr/bin/env bash

INSTALLATION_ID="$(uuidgen)"
ACCESS_TOKEN="eyJ0eXAiOiJKV1QiLCJhbGciOiJIUzI1NiJ9.eyJpc3MiOiJodHRwczovL2FwaS5naG9zdHZyLmlvIiwiaWF0IjoxNDc0ODkwNzI2LCJleHAiOjE1MDY0MjY3MjYsImF1ZCI6Imh0dHBzOi8vYXBpLmdob3N0dnIuaW8iLCJzdWIiOiJ0ZXN0In0.v1Koc1odHfLAVFl1RrTEQrzKVUdEo_BzqnRqkEcMfMA"

curl() {
	(>&2 echo "DEBUG CURL:" "$@")
	command curl "$@" | tee /dev/stderr
}


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

curl -kv 'https://api.ghostvr.io/v1/session'                   \
-H "Authorization: Bearer $ACCESS_TOKEN"            \
-H 'Content-Type: application/json; charset=UTF-8'  \
--data "$SESSION" 


