system "l log.q";

.replayer.init:{
  .replayer.initArguments[];
  .replayer.initConnections[];
  .replayer.initSchemas[];
  };

.replayer.initArguments:{
  .log.info["Initializing Replayer Arguments..."];
  defaultargs:(!) . flip (
    (`tphostport  ; 7001);
    (`tplogfile   ; `$"resources/replay.tplog");
    (`start       ; 20:45:00.000);
    (`end         ; 21:00:00.000);
    (`interval    ; 100);
    (`startover   ; 0b )
    );
  `args set .Q.def[defaultargs] .Q.opt[.z.x];
  .log.info["Replayer Arguments Initialized!"];
  };

.replayer.initConnections:{
  .log.info["Initializing Connection..."];
  system "l connection.q";

  address:hsym `$"unix://",string[args`tphostport];
  .conn.open[`tp;address;enlist[`lazy]!enlist 0b];
  .log.info["Connection Initialized!"];
  };

.replayer.initSchemas:{
  .log.info["Initializing Schemas..."];
  system "l schema.q";
  {x set `kdbRecvTime xcols update kdbRecvTime:`timestamp$() from value x} each tables`.;
  {if[`sym in cols x;update `g#sym from x]}each tables[];
  .log.info["Schemas Initialized!"];
  };

.replayer.initTimer:{
  .log.info["Initializing Timer..."];
  system "l timer.q";
  .timer.addPeriodicTimer[{.replayer.periodic[]};args`interval];
  .log.info["Timer Initialized!"];
  };

.replayer.completed:0b;
.replayer.stopped:0b;

.replayer.timecols:`tradetime`quotetime;
.replayer.curtime:-1;
.replayer.starttime:-1;
.replayer.endtime:-1;

.replayer.load:{
  .log.info"Loading TP Log File...";
  if[()~key hsym args[`tplogfile];'"Log file does not exist!"];
  
  -11!hsym args[`tplogfile];
  .log.info"Tuning Replay Data...";
  {
    if[0<count value x;
      update timej:`long$(args[`interval] xbar kdbRecvTime.time) from x;
      starttimej:first (flip 1#value x)`timej;
      if[any(.replayer.starttime=-1;.replayer.starttime>starttimej);.replayer.starttime:starttimej];
      
      endtimej:first (flip -1#value x)`timej;
      if[any(.replayer.endtime=-1;.replayer.endtime<endtimej);.replayer.endtime:endtimej];
    ];
    delete kdbRecvTime from x;
    delete tradedirection from x;
    delete isirregular from x;
  } each tables[];

  .log.info["Start Time:",-3!`time$.replayer.starttime];
  .replayer.curtime:.replayer.starttime;
  .log.info"TP Log File Loaded!";
  };

upd:{[table;data]
  if[table in `trade`quote;
    data:$[0>type first data;enlist cols[table]!data;flip cols[table]!data];
    data:delete from data where not kdbRecvTime.time within (args[`start];args[`end]);
    if[0<count data;insert[table;data]];
  ];
  };


.replayer.periodic:{
  if[.replayer.completed; :()];
  if[.replayer.stopped; :()];
  {  
    if[0=count value x; :()];
    data:delete timej from select from x where timej=.replayer.curtime;
    if[0=count data; :()];
    timecol:.replayer.timecols inter cols x;
    if[0<count timecol;data:![data;();0b;(enlist first timecol)!(enlist .z.t)]];
    .conn.asyncSend[`tp;(`.u.upd;x;value flip data)];
    } each tables[];

    .replayer.curtime+:args[`interval];
    if[.replayer.curtime>.replayer.endtime;
      $[args[`startover];
        [
          .replayer.curtime:.replayer.starttime;
          .conn.syncSend[`tp;(`.u.end;.z.d)];
          .log.info["Replayer Starting Over..."]
        ];
        if[not .replayer.completed;
          .log.info["replay completed, will not start over..."];
          .replayer.completed:1b
        ]
      ]
    ];
  };

.replayer.init[];
.replayer.load[];
.replayer.initTimer[];
