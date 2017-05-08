var express = require("express");
require("express-jsend");
var bodyParser = require("body-parser");
var app = express()
app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

var Token = {
	tokens : {},
	status: function(token) {
		if(!(token in this.tokens))
			return "pending";
		return this.tokens[token];
	},
	reject: function(token) {
		this.tokens[token] = "rejected";
	},
	accept: function(token) {
		if(!this.status(token) == "pending")
			throw "token must be pending to accept";
		this.tokens[token] = "ok";
	}
}

function extractToken(req) {
	var auth = req.header("Authorization");
	if(auth) {
		let token = auth.split(" ")[1];
		log("token from header: " + token)
		return token;
	}
	return req.params.access_token || req.query.access_token || req.body.access_token;
}

function log() {
	console.log([].slice.call(arguments).map(function(o) { return (o.substr)?o:JSON.stringify(o, undefined, 2); }).join(" "));
}

app.get("/", function (req, res) {
	res.send(JSON.stringify(Token.tokens));
});

app.get("/mock/accept/:token", function(req,res) {
	log("mock:accept("+req.params.token+")");
	Token.accept(req.params.token);
	res.send("OK");
});

app.get("/mock/reject/:token", function(req,res) {
	log("mock:reject("+req.params.token+")");
	Token.reject(req.params.token);
	res.send("OK");
});

app.all("/mock/test", function(req,res) {
	log("mock:test");
	var token = extractToken(req);
	var info = {
		body:req.body,
		params:req.params,
		headers:req.headers,
		token:req.token
	};
	log(info);
	res.jsend({mock:true, info:info});
});

app.get("/mock/follow_up/:video_id", function(req,res) {
	log("mock:follow_up");
	var info = {
		body:req.body,
		params:req.params,
		headers:req.headers,
		token:req.token
	};
	log(info);
	res.send(
		"FOLLOW UP for "+req.params.video_id
		+'<button onclick="window.external.ActionCompleted()">complete</button>'
		+'<button onclick="window.external.ActionCanceled()">cancel</button>'
	);
});

app.get("/api/v1/authorize_player", function(req,res) {
	var token=extractToken(req);
	var installation_id=req.params.installation_id;
	var player=req.params.player;
	log("authorize_player(" + token + ")");
	res.send(
		JSON.stringify({token:token, installation_id:installation_id, player:player})
		+'<br/><a href="/mock/accept/'+token+'">accept</a>'
		+'/ <a href="/mock/reject/'+token+'">reject</a>'
	);
});

function requireValidToken(req,res,next) {
	var token = extractToken(req);
	console.log("require valid token, token = " + token + " status=" + Token.status(token));
	if(Token.status(token) != "ok")
		res.jerror("token not ok");
	else
		next();
}

app.post("/api/v1/verify_player_token", function(req,res) {
	var token=extractToken(req);
	log("verify_player_token(" + token + ") -> " + Token.status(token));
	res.jsend({ "verification_status": Token.status(token), "team_name": "mock" })
});

app.post("/api/v1/discard_player_token", requireValidToken, function(req,res) {
	var token=extractToken(req);
	log("discard_player_token(" + token + ")");
	Token.reject(token);
	res.send("OK");
});

app.post("/api/v1/video_session", requireValidToken, function(req,res) {
	var token=extractToken(req);
	log("video_session");
	res.jsend({ follow_up: "http://localhost:3000/mock/follow_up/" + req.body.guid });
});

app.listen(3000, function () {
  console.log("Example app listening on port 3000!")
});