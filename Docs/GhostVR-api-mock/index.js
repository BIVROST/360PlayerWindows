var express = require("express");
require("express-jsend");
var bodyParser = require("body-parser");
var app = express()
app.use(bodyParser.urlencoded({ extended: false }));
app.use(bodyParser.json());

var Token = {
	tokens : {},
	STATUS_OK : "ok",
	STATUS_REJECTED: "rejected",
	STATUS_PENDING: "pending",
	status: function(token) {
		if(!(token in this.tokens))
			return Token.STATUS_PENDING;
		return this.tokens[token];
	},
	reject: function(token) {
		if(!this.status(token) == Token.STATUS_REJECTED)
			throw "token must already rejected";
		this.tokens[token] = Token.STATUS_REJECTED;
	},
	accept: function(token) {
		if(!this.status(token) == Token.STATUS_PENDING)
			throw "token must be pending to accept";
		this.tokens[token] = Token.STATUS_OK;
	}
}

function extractToken(req) {
	var auth = req.header("Authorization");
	if(auth) {
		var token = auth.split(" ")[1];
		log("token from header: " + token)
		return token;
	}
	return req.params.access_token || req.query.access_token || req.body.access_token;
}

function log() {
	console.log([].slice.call(arguments).map(function(o) { return (o.substr)?o:JSON.stringify(o, undefined, 2); }).join(" "));
}

function requireValidTokenJsend(req,res,next) {
	var token = extractToken(req);
	console.log("jsend require valid token, token=" + token + " status=" + Token.status(token));
	if(Token.status(token) != Token.STATUS_OK) {
		console.log("   rejected");
		res.jerror("token not ok");
	}
	else {
		console.log("   accepted");
		next();
	}
}

function requireValidTokenHtml(req,res,next) {
	var token = extractToken(req);
	console.log("html require valid token, token=" + token + " status=" + Token.status(token));
	if(Token.status(token) != Token.STATUS_OK) {
		console.log("   rejected");
		res.status(401);
		res.send("token not ok");
	}
	else {
		console.log("   accepted");
		next();
	}
}

app.get("/", function (req, res) {
	res.send(Object.keys(Token.tokens).map(function(t){ 
		var st=Token.status(t); 
		return '<p>' + t + ' (' + st + ') <a href="/mock/reject/' + t +'">reject</a> <a href="/mock/accept/' + t +'">accept</a></p>' ;
	}).join("\n"));
});

app.get("/mock/accept/:access_token", function(req,res) {
	log("mock:accept("+req.params.access_token+")");
	Token.accept(req.params.access_token);
	res.send("OK");
});

app.get("/mock/reject/:access_token", requireValidTokenHtml, function(req,res) {
	log("mock:reject("+req.params.access_token+")");
	Token.reject(req.params.access_token);
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
	res.send(
		"FOLLOW UP for "+req.params.video_id
		+'\n<a href="/mock/follow_up/'+req.params.video_id+'/completed">completed</a>'
		+'\n<a href="/mock/follow_up/'+req.params.video_id+'/canceled">canceled</a>'
		+'\n<hr /><button onclick="errrr()">js error</button>'
		+'\n<button onclick="(function(xhr) { xhr.open(\'get\', \'/xherrrr\'); xhr.send(); } )(new XMLHttpRequest());">xhr error local</button>'
		+'\n<button onclick="(function(xhr) { xhr.open(\'get\', \'http://xherrrr.com\'); xhr.send(); } )(new XMLHttpRequest());">xhr error nxdomain</button>'
		+'\n<a href="http://errrrrrrrrrrrrrrrrr.com">navigation error nxdomain</a>'
		+'\n<a href="/errrr">navigation error 404</a>'
		+'\n<button onclick="(function(){ for(var i=1000; i--; ) { var p=document.createElement(\'span\'); p.appendChild(document.createTextNode(\'All work and no play makes Jack a very dull boy. \')); document.body.appendChild(p);} })()">wall of text</button>'
	);
});

app.get("/mock/follow_up/:video_id/:decision", function(req,res) {
	log("mock:follow_up decision " + req.params.decision);
	switch(req.params.decision) {
		case "completed":
			res.send('<script type="text/javascript">window.external.ActionCompleted()</script>');
			break;
		case "canceled":
			res.send('<script type="text/javascript">window.external.ActionCompleted()</script>');
			break;
		default:
			throw "decision error";
	}
});

app.get("/api/v1/authorize_player", function(req,res) {
	console.log(req);
	var token=extractToken(req);
	var installation_id=req.query.installation_id;
	var player=req.query	.player;
	log("authorize_player(" + token + ")");
	res.send(
		JSON.stringify({token:token, installation_id:installation_id, player:player})
		+' <a href="/mock/accept/'+token+'">accept</a>'
		+' <a href="/mock/reject/'+token+'">reject</a>'
	);
});

app.post("/api/v1/verify_player_token", function(req,res) {
	var token=extractToken(req);
	log("verify_player_token(" + token + ") -> " + Token.status(token));
	res.jsend({ "verification_status": Token.status(token), "team_name": "mock" })
});

app.post("/api/v1/discard_player_token", function(req,res) {
	var token=extractToken(req);
	log("discard_player_token(" + token + ")");
	Token.reject(token);
	res.send("OK");
});

app.post("/api/v1/video_session", requireValidTokenJsend, function(req,res) {
	var token=extractToken(req);
	log("video_session");
	res.jsend({ follow_up: "http://localhost:3000/mock/follow_up/" + req.body.guid });
});

app.listen(3000, function () {
  console.log("Example app listening on port 3000!")
});