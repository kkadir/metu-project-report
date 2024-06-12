system "l log.q";

.gw.init:{
  .gw.initArguments[];

  system"p ",string[args`gwhostport];

  .gw.initLibraries[];
  .gw.initCaches[];
  .gw.initConnections[];

  upd::.gw.priv.broadcast;
  end::.gw.priv.end;
  };

.gw.initArguments:{
  .log.info["Initializing Gateway Arguments..."];
  defaultargs:(!) . flip (
    (`tphostport   ; `7001);
    (`ctphostport  ; `7002);
    (`gwhostport   ; `8001)
    );
  `args set .Q.def[defaultargs] .Q.opt[.z.x];
  .log.info["Gateway Arguments Initialized!"];
  };

.gw.initLibraries:{
  .log.info["Initializing Gateway Libraries..."];
  system "l timer.q";
  system "l connection.q";
  system "l u.q";
  system "l type.q";

  .log.info["Gateway Libraries Initialized!"];
  };

.gw.initCaches:{
  .gw.priv.whiteHandles:enlist 0;
  .gw.priv.services:([serviceId:`guid$()]serviceType:`symbol$();serviceHandle:`int$();serviceLoad:`long$());
  .gw.priv.users:([userId:`guid$()]userIp:();userHandle:`int$();connTime:`timestamp$();resType:`$());
  .gw.priv.subs:([ userId:`.gw.priv.users$enlist 0ng; subSym:`g#enlist`; subTopic:`g#enlist`] requestSym:enlist `;subId:enlist 0Ng;serviceId:`.gw.priv.services$enlist 0Ng;subFeed:enlist enlist 0Ni;ref:enlist enlist"";includeLatencyStats:enlist 0b );
  .gw.priv.supportedSubTopics:`trade`bbo`nbbo;
  };

.z.po:{[handle]
  /.gw.priv.whiteHandles,:handle;
  .gw.priv.registerUser[`;handle;`kdb];
  /.conn.priv.Zpo[{};handle];
  };

.z.wo:{[handle]
  .gw.priv.registerUser[`;handle;`kdb];
  /.conn.priv.Zwo[{};handle];
  };

.z.pg:{[cmd]
  .log.info["Received command pg: ",string cmd];
  if[.z.w in .gw.priv.whiteHandles; :value cmd];
  resType:.gw.priv.resType[cmd];
  cmdRes:.gw.priv.stdCmd[cmd];
    
  func:cmdRes[0];
  params:cmdRes[1];

  if[not `ref in key params;
    .gw.priv.safeSend[neg[.z.w]] .gw.priv.convert[resType] `error`ref!("No ref provided";"");
    :()
  ];

  ref:params[`ref]:16 sublist .type.ensureString[params[`ref]];
  user:first select from .gw.priv.users where userHandle=.z.w;

  .gw.priv.convert[resType] .gw.priv.runSafeCmd[func;params;ref]
  };

.z.ws:.z.ps:{[cmd]
  if[.z.w in .gw.priv.whiteHandles; :value cmd;];
  resType:.gw.priv.resType[cmd];
  cmdRes:@[.gw.priv.stdCmd;cmd;{[resType;error]
    neg[.z.w] .gw.priv.convert[resType] `error`ref!(error;"");
    }[resType]];
  if[not count cmdRes; :()];
  func:cmdRes[0];
  params:cmdRes[1];

  if[not `ref in key params;
    .gw.priv.safeSend[neg[.z.w]] .gw.priv.convert[resType] `error`ref!("No ref provided";"");
    :()
  ];

  ref:params[`ref]:16 sublist .type.ensureString[params[`ref]];
  user:first select from .gw.priv.users where userHandle=.z.w;
  
  .gw.priv.safeSend[neg[.z.w]] .gw.priv.convert[resType] .[.gw.priv.runSafeCmd;(func;params;ref);{`ref`error!(x;y)}[ref]];
  };

.z.pc:{[handle]
  .log.info["Client disconnected: "];
  .gw.priv.whiteHandles _:.gw.priv.whiteHandles?handle;
  .gw.priv.removeUser[handle];
  /.conn.priv.Zpc[{};handle];
  };

.z.wc:{[handle]
  .log.info["Client disconnected: "];
  .gw.priv.whiteHandles _:.gw.priv.whiteHandles?handle;
  .gw.priv.removeUser[handle];
  /.conn.priv.Zwc[{};handle];
  };

system"x .z.ph"; 

.gw.priv.registerUser:{[username;handle;resType]
  newUser: ([userId: -1?0ng] userIp: enlist "." sv string"h"$0x0 vs .z.a;userHandle: enlist handle; connTime: enlist .z.p; resType: enlist resType);
  .log.info["New User: ",.j.j newUser];
  `.gw.priv.users upsert newUser;
  };

.gw.priv.removeUser:{[handle]
    .log.info["User with handle: ",(string handle)," has disconnected. Removing subscriptions."];
    
    subs:0!select from .gw.priv.subs where userId.userHandle = handle;
    
    .gw.priv.unsubscribe ./: flip value flip `subTopic`subSym`userHandle xcols 0!select subSym by subTopic, userId.userHandle from subs;
    if[handle in exec distinct serviceHandle from .gw.priv.services;
        .gw.priv.removeService[handle]
    ];
    user:first 0!select from .gw.priv.users where userHandle = handle;
    .log.debug"Removing user ",string[user`userName]," from .gw.priv.subs and  .gw.priv.users";
    delete from `.gw.priv.subs where userId.userHandle = handle;
    update userHandle:0Ni from `.gw.priv.users where userHandle = handle;
    .entitle.postLogout[user[`userName];user[`userIp]];
 };

.gw.priv.resType:{[cmd]
  $[.type.isByteList[cmd];`byte;
    .type.isString[cmd];`json;
    `kdb
  ]
  };

.gw.priv.stdCmd:{[cmd]
  if[.type.isByteList[cmd];cmd:@[-8!;cmd;{'"Serialized request is unreadable!"}];];
  if[.type.isString[cmd];cmd:@[.j.k;cmd;{'"JSON request is unreadable!"}];];
  if[not 2 = count cmd;'"Request format is incorrect!"];
   
  func:.type.ensureSymbol[cmd[0]];
  if[not .type.isSymbol[func];'"Requests function must be a symbol!"];
  
  params: cmd[1];
  if[not .type.isDict[params];'"Requests params must be dictionary!";];
  :(func;params);
  };

.gw.priv.runSafeCmd:{[func;params;ref]
  .gw.priv.cmdMap:`subscribe`unsubscribe!(.gw.subscribe;.gw.unsubscribe);
  
  if[null .gw.priv.cmdMap[func];'"Only the following commands are supported: ", .j.j key .gw.priv.cmdMap];
  if[not .type.isDict[params]; '"The params must be passed in object/dictionary form!"];
    
  .[.gw.priv.cmdMap[func];(params;ref);{.log.error[x];'x}]
 };

.gw.priv.convert:{[resType;data]
  $[resType = `byte;-9!data;
    resType = `json;.j.j data;
    data
  ]
  };

.gw.priv.safeSend:{[handle;data]
  @[handle;data;{[handle;error]
    .log.error["Failed to publish data to handle ",(-3!handle),": ", error]
  }[handle]];
  };

.gw.priv.registerService:{[service]
  newService: ([serviceId: -1?0ng] serviceType: enlist service; serviceHandle: enlist .z.w; serviceLoad: enlist 0);
  .log.info["New Service: ",.j.j newService];
  `.gw.priv.services upsert newService;
  :key newService;
  };

.gw.priv.chooseService:{[topic;service]
  :1 sublist 0! select from .gw.priv.services where serviceType=service, serviceLoad = min serviceLoad
  };

 .gw.priv.removeService:{[handle]
    //mark service as disconnected instead of removing to maintain referential integrity
    update serviceType:`disconnected from `.gw.priv.services where serviceHandle = handle;
    disconnectedService:first select from .gw.priv.services where serviceHandle = handle;
    // check for any subscritpions to this topic
    svcSubs:0!select from .gw.priv.subs where serviceId.serviceHandle = handle;
    svcSubs:update userHandle:handle from svcSubs;
    if[count svcSubs;
        availableSvcs:select from .gw.priv.services where serviceType = first svcSubs[`serviceType],asset=disconnectedService`asset,region=disconnectedService`region;
        $[count availableSvcs;
            .gw.priv.subscribe each svcSubs;
            [
                {[sub]
                    neg[sub[`userId].userHandle] @\:
                    .gw.priv.convert[sub[`userId].resType] (`topic`error`ref)!(sub[`subTopic];"No available services for existing subscription";sub[`ref]);
                } each svcSubs;
                delete from `.gw.priv.subs where subId in svcSubs[`subId];
                update `g#subSym,`g#subTopic from `.gw.priv.subs;
            ]
        ];
    ];
  };

.gw.priv.subscribe:{[params]
  topic:params`topic;
  syms:params`syms;
  service:params`service; 
  ref:params`ref;
  
  service:.gw.priv.chooseService[topic;service];
  if[not count service;:(0b;"No services available to process this request");];
  
  user:params[`user]:first 0!select from .gw.priv.users where userHandle= params[`userHandle];
  noInRequest:count params[`subId];
  
  pp::params;
  newSub:([userId:params[`user]`userId; subSym:syms; subTopic: topic] 
    requestSym:params[`reqSyms]; 
    subId: params[`subId]; 
    serviceId:`.gw.priv.services$first service[`serviceId]; 
    subFeed:params[`subFeeds]; 
    ref:noInRequest#enlist ref;
    includeLatencyStats:noInRequest#enlist params[`includeLatencyStats]
  );
  newSubHandle:exec first serviceId.serviceHandle from newSub;
  `.gw.priv.subs upsert newSub;
  deltaTopic:$[topic in `nbbo`bbo;`quote;topic];
  symbolsOfInterest:exec distinct subSym from .gw.priv.subs where subTopic in ?[topic in `nbbo`bbo;`nbbo`bbo;(),topic], serviceId.serviceHandle=newSubHandle;
  if[`=first symbolsOfInterest;symbolsOfInterest:`];
  {[newSubHandle;deltaTopic;symbolsOfInterest] (neg newSubHandle)(`.u.sub;deltaTopic;symbolsOfInterest)}[newSubHandle;;symbolsOfInterest]each (),deltaTopic;
  update serviceLoad: serviceLoad + 1 from `.gw.priv.services where serviceId in service[`serviceId];

  :(1b;"Subscribed successfully");
  };

.gw.priv.unsubscribe:{[topic;syms;usrHandle]
  .log.info".gw.priv.unsubscribe[",(-3!topic),";",(-3!syms),";",(-3!usrHandle),"]";
  
  thisSub:0!select from .gw.priv.subs where subTopic=topic, subSym in syms, userId.userHandle=usrHandle;
  otherSub:0!select from .gw.priv.subs where subTopic=topic, subSym in syms, userId.userHandle<>usrHandle;
  user:first 0!select from .gw.priv.users where userHandle = usrHandle;
  
  if[0 < count thisSub;
    serviceHandles:exec distinct serviceId.serviceHandle from thisSub;
    topicsToDrop:(),topic;
    
    delete from `.gw.priv.subs where subTopic in topicsToDrop, subSym in syms, userId.userHandle = usrHandle;
    update `g#subSym,`g#subTopic from `.gw.priv.subs;
    
    if[all(`=first syms;0<count otherSub); .log.info["Global syms subscription: don't unsubscribe"]; :()];
    
    {[topic;handle]
      ptopic:$[topic in `bbo`nbbo;`quote;topic];
      ptopics:$[topic in `nbbo`bbo;`nbbo`bbo;(),topic];
      psyms:exec distinct subSym from .gw.priv.subs where subTopic in ptopics, serviceId.serviceHandle=handle;
      .log.debug["Subscribing topic:",(-3!ptopic)," subtopics:",(-3!ptopics)," syms:",(-3!psyms)];
      (neg handle)(`.u.sub;ptopic;psyms)
    }[topic] each serviceHandles;
  ];
  };

.gw.priv.publish:{[user;topic;data;ref;params]
  additionalReturnFields:()!();
  if[1b~params`includeLatencyStats;
    available:`lastRecvTime`publishTime inter key params;
    additionalReturnFields[available]:params available;
    additionalReturnFields[`publishTime]:.z.p;
  ];
  if[`error in key params;
    additionalReturnFields[`error`errorMessage]:params[`error`errorMessage];
  ];
  .gw.priv.safeSend[;.gw.priv.convert[user[`resType]] (`topic`data`ref!(topic;data;ref)),additionalReturnFields] each neg[user[`userHandle]];
  };

.gw.priv.broadcast:{[topic;data]
  strms:$[topic=`quote;`nbbo`bbo;enlist topic];
  dsyms:distinct data[`sym];
  users:select distinct subSym by userId.userHandle,userId.resType,subTopic,includeLatencyStats from .gw.priv.subs where subTopic in strms,any(subSym=`;subSym in dsyms),serviceId.serviceHandle=.z.w;
  {[user;data]
    lastRecvTime:$[includeLatencyStats:user`includeLatencyStats;last data`kdbRecvTime;0Np];
    data:?[data;enlist(in;`sym;(`user;enlist`subSym));0b;()!()];
    .gw.priv.publish[user;user[`subTopic];data;"streaming_update";`includeLatencyStats`lastRecvTime`usePaging!(includeLatencyStats;lastRecvTime;0b)];
  }[;data] each 0!users;
  };

.gw.unsubscribe:{[params;ref]
  params[`topic]:.type.ensureSymbol[params[`topic]];
  topic:params[`topic];
  syms:.gw.priv.validateSyms[params];    
  ref:16 sublist .type.ensureString[ref];
  
  if[syms ~ `;syms:exec distinct subSym from .gw.priv.subs where subTopic=topic, userId.userHandle=.z.w];
  if[topic ~ `;topic:exec distinct subTopic from .gw.priv.subs where subTopic=topic, userId.userHandle=.z.w];

  .gw.priv.unsubscribe[topic;syms;.z.w];
  :(`unsubscribe`ref)!(1b;ref);
  };

.gw.subscribe:{[params;ref]
  params:.gw.priv.buildSubscribeParams[params;ref];
  params:.gw.priv.findServiceForRequest[params];
  params[`subId]:(neg count params[`syms])?0ng;
  params[`reqSyms]:params[`syms];
  params[`subFeeds]:(count params[`syms])#`feed;

  subOutput:@[.gw.priv.subscribe;params;{.log.error msg:"Subscription failed: ",x;(0b;msg)}];
  :(`subscribe`msg`ref)!(subOutput,enlist params[`ref]);
  };

.gw.priv.buildSubscribeParams:{[params;ref]
  params:(enlist[`]!enlist(::)),params;
  params[`topic]:.type.ensureSymbol[params[`topic]];

  if[not params[`topic] in .gw.priv.supportedSubTopics;'"Unsupported topic to sub:",(-3!params[`topic])];

  params[`syms]:.gw.priv.validateSyms[params];
  params[`ref]:100 sublist .type.ensureString[ref];
  params[`userHandle]:.z.w;
  params[`includeLatencyStats]:$[`includeLatencyStats in key params;params`includeLatencyStats;0b];
  :params;
  }; 

.gw.priv.findServiceForRequest:{[params]
  params[`service]:`ctp;
  :params;
  };

.gw.priv.validateSyms:{[params]
  syms:(),$[.type.isString[params[`syms]];
    .type.ensureSymbol[params[`syms]];
    .type.ensureSymbol each params[`syms]
  ];
  :(),syms;
  };



.gw.initConnections:{
  .conn.open[`ctp;hsym `$"unix://",string[args`ctphostport];`lazy`ccb!(0b;.gw.priv.connectedServiceCallback)];
  };

.gw.priv.connectedServiceCallback:{
  handle:.conn.list[][x][`fd];
  .gw.priv.whiteHandles,:handle;
  .gw.priv.registerUser[`root;handle;`kdb];
  .conn.asyncSend[x]({(neg .z.w)(`.gw.priv.registerService;`ctp)};`);
  };

.gw.priv.end:{[date]
  .log.info"Calling .u.end as .gw.priv.end for date ",string date;
  };

.gw.init[];