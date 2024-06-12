system "l log.q"

.conn.priv.connections:([name:`$()]
    lazy:`boolean$();
    fd:`int$();
    addresses:();
    timeout:`long$();
    ccb:();
    dcb:();
    rcb:();
    ecb:()
  );

.conn.list:{.conn.priv.connections};

.conn.priv.default:`fd`lazy`ccb`dcb`rcb`ecb!(0N;0b;(::);(::);(::);(::));
.conn.timeout:100;
.conn.priv.minbackoff:500;
.conn.priv.maxbackoff:1500;

.conn.trap:@[;;];
.conn.priv.defaulterrcb:{[name;address;error]
  .log.error["Connection Error: ",string[name]," - ",-3!address,": ",error];
  };

.conn.priv.resolveerrcb:{[name;address;error]
  .log.error["Resolve Error: ",string[name]," - ",-3!address,": ",error];
  };

.conn.priv.ccberr:{[name;error]
  .log.error["Connection Callback Error: ",string[name],": ",error];
  };

.conn.priv.dcberr:{[name;error]
  .log.error["Disconnection Callback Error: ",string[name],": ",error];
  };

.conn.priv.rcberr:{[name;error]
  .log.error["Registration Callback Error: ",string[name],": ",error];
  ()!();
  };

.conn.priv.filedescriptor:{[name]
  if[-11h<>type name;'"Invalid Name Type"];
  if[not name in exec name from .conn.priv.connections;'"Connection Not Found"];

  if[null fd:.conn.priv.connections[name;`fd];
    if[.conn.priv.connections[name;`lazy];
      fd:.conn.priv.attempt[name];
    ];
    if[null fd;'"Connection not valid: ",string name];
  ];
  fd
  };

.conn.open:{[name;addresses;options]
  if[type[addresses] in -11 10h; addresses:enlist addresses];
  if[11h=type addresses; addresses:string addresses];
  connection:.conn.priv.default,options,`name`addresses!(name;addresses);
  if[not `timeout in key connection; connection[`timeout]:.conn.timeout];
  if[-11h<>type connection`name;'"Invalid Name Type"];
  if[connection[`name] in exec name from .conn.priv.connections;'"Name Already Exists"];
  extra:(key[connection] except cols[.conn.priv.connections]);
  if[0<count extra;'"Unknown Options: ",","sv string extra;];

  `.conn.priv.connections upsert connection;

  .log.info["Opening Connection: ",-3!name];

  if[not connection`lazy;.conn.priv.attempt[name];];
  };

.conn.close:{[name]
  if[-11h<>type name;'"Invalid Name Type"];
  if[not name in exec name from .conn.priv.connections;'"Connection Not Found"];
    
  if[not null h:.conn.priv.connections[name;`fd];hclose h];

  delete from `.conn.priv.connections where name=name;
  };

.conn.priv.attempt:{[name]
  connection:.conn.priv.connections[name];
  addresses:connection`addresses;
  fd:connection`fd;
  ecb:connection`ecb;
  if[ecb~(::); ecb:.conn.priv.defaulterrcb];

  n:count addresses;
  i:0;
  while[null[fd] and i<n;
    address:addresses[i];
    resolvedaddresses:@[enlist;address;.conn.priv.resolveerrcb[name;address;]];

    while[null[fd] and 0<count resolvedaddresses;
      resolvedaddress:resolvedaddresses 0;
      resolvedaddresses:1_resolvedaddresses;

      .log.info["Attempting Connection: ",string[name]," - ",resolvedaddress];
      if[not null fd:.conn.trap[hopen;resolvedaddress;'[{0Ni};]ecb[name;address;]];
        resolvedaddresses:();

        .log.info["Connected: ",string[name]," - ",resolvedaddress];
        .conn.priv.connections[name;`fd]:fd;

        .conn.trap[{.conn.priv.connections[x;`ccb][x]};name;.conn.priv.ccberr[name;]];
      ];
    ];
  ];
  i+:1;
  };

.conn.syncSend:{[name;data]
    fd:.conn.priv.filedescriptor[name];
    fd data};

.conn.asyncSend:{[name;data]
    fd:.conn.priv.filedescriptor[name];
    neg[fd] data};