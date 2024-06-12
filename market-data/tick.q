system "l log.q";

.tick.init:{
  .tick.initArguments[];
  .tick.initLibraries[];
  .tick.initSchemas[];

  system"p ",string[args`tphostport];
  .u.tt:args`tptime;

  .u.tick[];
  };

.tick.initArguments:{
  .log.info["Initializing Ticker Arguments..."];
  defaultargs:(!) . flip (
    (`tphostport  ; 7001);
    (`tptime      ; 0)
    );
  `args set .Q.def[defaultargs] .Q.opt[.z.x];
  .log.info["Ticker Arguments Initialized!"];
  };

.tick.initLibraries:{
  .log.info["Initializing Ticker Libraries..."];
  system "l timer.q";
  system "l connection.q";
  system "l u.q";

  .log.info["Ticker Libraries Initialized!"];
  };

.tick.initSchemas:{
  .log.info["Initializing Schemas..."];
  system "l schema.q";
  {x set `kdbRecvTime xcols update kdbRecvTime:`timestamp$() from value x} each tables`.;
  {delete tradedirection from x;delete isirregular from x;} each tables`.;
  .log.info["Schemas Initialized!"];
  };

\d .u

tick:{
  init[];
  @[;`sym;`g#]each t;
  };

upd:{[t;x]
  a:.z.p;
  if[not -12=type first first x;x:$[0>type first x;a,x;(enlist(count first x)#a),x];];
  f:key flip value t;
  pub[t;$[0>type first x;enlist f!x;flip f!x]];
  };

\d .

.tick.init[];
/.timer.addPeriodicTimer[{.u.periodic[]};$[.u.tt;.u.tt;1000i]];