system "l log.q";

.ctp.init:{
  .ctp.initArguments[];

  system"p ",string[args`ctphostport];

  .ctp.initLibraries[];
  .ctp.initTimersUpdates[];
  .ctp.initConnections[];
  };

.ctp.initArguments:{
  .log.info["Initializing Chained-Tickerplant Arguments..."];
  defaultargs:(!) . flip (
    (`tphostport  ; `7001);
    (`ctphostport  ; `7002);
    (`ctptime     ; 250)
    );
  `args set .Q.def[defaultargs] .Q.opt[.z.x];
  .log.info["Chained-Tickerplant Arguments Initialized!"];
  };

.ctp.initLibraries:{
  .log.info["Initializing Chained-Tickerplant Libraries..."];
  system "l timer.q";
  system "l connection.q";
  system "l u.q";

  .log.info["Chained-Tickerplant Libraries Initialized!"];
  };

.ctp.initTimersUpdates:{
  .log.info["Initializing Chained-Tickerplant Timers & Updates..."];
  .ctp.period:args`ctptime;
  `upd set .ctp.upd;
  .z.ts:.ctp.pub;
  system["t ",string .ctp.period];

  .log.info["Chained-Tickerplant Timers & Updates Initialized!"];
  };

.ctp.initConnections:{
  .u.rep:.ctp.rep;
  .u.end:.ctp.end;
  .conn.open[`tp;hsym `$"unix://",string[args`tphostport];`lazy`ccb!(0b;{.u.init .u.rep @ .conn.syncSend[`tp]"(.u.sub[`;`])"})];
  };

.ctp.pub:{
  .u.pub'[.ctp.tables;value each .ctp.tables];
  @[`.;.ctp.tables;@[;`sym;`g#]0#];
  };

.ctp.rep:{
  (.[;();:;].)each x;
  };

.ctp.end:{
  (neg union/[.u.w[;;0]])@\:(`.u.end;dt);
  };


.ctp.upd:{[t;x] t insert x;};
.ctp.tables:();
.ctp.init[];
.ctp.tables:tables[];