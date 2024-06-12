//Want to protect the definition of these variables if script is reloaded in the same session.
if[not `idcount in key `.timer.priv;
    .timer.priv.idcount:0];
if[not `timers in key `.timer.priv;
    .timer.priv.timers:([id:`int$()] when:`timestamp$(); func:(); period:`timespan$();catchUpMode:`$())];

//these should be in util
.util.trp:{[fun;params;errorHandler] -105!(fun;params;errorHandler)};
.util.try2:{[fun;params;errorHandler] .util.trp[fun;params;{[errorHandler;e;t] -2"Error: ",e," Backtrace:\n",.Q.sbt t; errorHandler[e]}[errorHandler]]};

.timer.errorlogfn:-2;
.timer.safeevalfn:.util.try2;

.timer.priv.FUNC_STR_MAX:1000
///
// Timer error handler. Can be replaced with user code.
// @param ctx A dictionary containing the timer details
// @param err Error
.timer.timerErrorHandler:{[ctx;err]
    funcStr:ssr[.Q.s1 ctx`func;"\n";""];
    if[.timer.priv.FUNC_STR_MAX<count funcStr;
        funcStr:((.timer.priv.FUNC_STR_MAX-2)#funcStr),".."];
    .timer.errorlogfn "timer got error ",err," from timer id=",string[ctx`id],", func=",funcStr;
    };

///
// Timer catch up mode. Determines what to do if a periodic timer takes longer to execute than its period.
// Possible values:
// `none: ignore the missed invocation - timer will run at the next occurrence
// `once: trigger missed invocations but multiple missed invocations are only triggered once
// `all: trigger all missed invocations - should only be used if the slowness is temporary and further invocations can indeed catch up
.timer.defaultCatchUpMode:`once;
.timer.priv.validCatchUpModes:`none`once`all;

.timer.priv.runCallback:{[ctx]
    //Exit early if timer is not registered.
    //This can happen if two timers are scheduled to run at the same time, and the first one to run removes the second.
    if[not ctx[`id] in exec id from .timer.priv.timers; :(::)];

    //Pass timer to the callback so it can use the ctx`id to remove itself if desired.
    //ctx`when can be used for the callback to know when it was supposed to be called so it can figure out if it's delayed.
   .timer.safeevalfn[ctx`func;enlist ctx;.timer.timerErrorHandler[ctx;]];

    //timer could have changed in the callback
    /ctx:exec from .timer.priv.timers where id=ctx`id;

    if[null ctx`id;
        :(::)];
    if[null ctx`period;
        delete from `.timer.priv.timers where id=ctx`id;
        :(::);
    ];
    now:.z.p;
    when:ctx`when;
    period:ctx[`period];
    when+:period;
    mode:ctx`catchUpMode;
    if[when<now;
        $[mode=`none;
            when+:period*ceiling (now-when)%period;
          mode=`all;
            ::;
          when+:period*(ceiling (now-when)%period)-1     //the "once" behavior which is also the default for invalid values
        ];
    ];
    .timer.priv.timers[ctx`id;`when]:when;
    };

.timer.priv.ONEDAYMILLIS:`int$24:00:00.000
//reset \t value for next timer, or zero if there aren't any
.timer.priv.setSystemT:{
    //only set timeout to zero if there are no more timers
    //Use ONEDAYMILLIS as max for timer to ensure int max not reached
    //.z.ts will wake up, have nothing to do and reset
    system "t ",string
      $[count when:asc exec when from .timer.priv.timers;
        min(.timer.priv.ONEDAYMILLIS;max(1;`int$`time$first[when]-.z.p));
        0];}

//check callback symbol points to a function
.timer.priv.validateCallback:{[callback]
    if[-11h=type callback;
         callback:get callback];
    if[not(type callback) in 100 104h;
     '`$"timer requires a func or projection."]}

.timer.priv.wrapCallbackByName: {[f]
    .timer.priv.validateCallback[f];
    $[-11h=type f;@[;]f;f]}

//replace callback function
.timer.replaceCallback:{[timerId;function]
    if[not type[timerId] in -6 -7h; '`$"Expecting a integer id in .timer.replaceCallback."];
    if[not timerId in exec id from .timer.priv.timers; '`$"invalid timer ID"];
    .timer.priv.timers[timerId;`func]:.timer.priv.wrapCallbackByName function;
    };

//insert a new timer
.timer.priv.addTimer:{[func;when;period]
    if[not null when; when:.timer.priv.toTimestamp when];
    if[not null period; period:.timer.priv.toTimespan period];
    id:.timer.priv.idcount+1;
    if[not .timer.defaultCatchUpMode in .timer.priv.validCatchUpModes;
        '`$".timer.defaultCatchUpMode has invalid value ",.Q.s1[.timer.defaultCatchUpMode],", should be one of ",.Q.s1 .timer.priv.validCatchUpModes;
    ];
    t:`id`when`func`period`catchUpMode!(id;when;func;period;.timer.defaultCatchUpMode);
    `.timer.priv.timers upsert t;
    .timer.priv.idcount+:1;
    .timer.priv.setSystemT[];
    id};

.timer.priv.NANOSINMILLI:1000*1000j;
.timer.priv.toTimespan:{
    $[-16h~t:type x; //timespan
        x;
      t in -6 -7h; //int, long = milliseconds
        `timespan$x*.timer.priv.NANOSINMILLI;
      t in -17 -18 -19h; //minute, second, time
        `timespan$x;
      '`$"cannot convert to timespan: ",.Q.s1 x]};

.timer.priv.toTimestamp:{
    $[-12h~t:type x; //timestamp
        x;
      -15h~t; //datetime
        `timestamp$x;
      t in -6 -7 -16 -17 -18 -19h; /int, long, timespan, minute, second, time
        (`timestamp$.z.d)+.timer.priv.toTimespan x;
      '`$"cannot convert to timestamp: ",.Q.s1 x]};

///
// Add a periodic timer with the specified start time.
// @param func The function to run
// @param when The first invocation time (timestamp)
// @param period The timer period (time or timespan)
// @return Timer handle
.timer.addPeriodicTimerWithStartTime:{[func;when;period]
    .timer.priv.addTimer[func;when;period]};

///
// Add a timer that runs once at the specified time. If the time is in the past, the function is run immediately after returning from currently running functions.
// @param func The function to run
// @param when The invocation time (timestamp)
// @return Timer handle
.timer.addAbsoluteTimer:{[func;when]
    .timer.priv.addTimer[func;when;0Nn]};

///
// Add a timer that runs once at the specified time. If the time is in the past, the function is not run.
// @param func The function to run
// @param when The invocation time (timestamp)
// @return Timer handle
.timer.addAbsoluteTimerFuture:{[func;when]
    $[.z.p<when:.timer.priv.toTimestamp when;.timer.priv.addTimer[func;when;0Nn];0N]};

///
// Add a periodic timer with the specified start time of day. If the time is in the future, it is run today, if it is in the past, it is run tomorrow.
// @param func The function to run
// @param startTime The first invocation time of day (time or timespan)
// @param period The timer period (time or timespan)
// @return Timer handle
.timer.addTimeOfDayTimer:{[func;startTime;period]
    firstTrigger:$[.z.t < startTime; .z.d+startTime; (.z.d+1)+startTime];
    .timer.addPeriodicTimerWithStartTime[func;firstTrigger;period]};

.timer.priv.relativeToTimestamp:{.z.p+.timer.priv.toTimespan x};

// Add a timer that runs once after a specified delay.
// @param func The function to run
// @param delay The time after which the timer runs (time or timespan)
// @return Timer handle
.timer.addRelativeTimer:{[func;delay]
    .timer.priv.addTimer[func;.timer.priv.relativeToTimestamp delay;0Nn]};

// Add a periodic timer.
// @param func The function to run
// @param period The timer period (time or timespan)
// @return Timer handle
.timer.addPeriodicTimer:{[func;period]
    .timer.priv.addTimer[func;.timer.priv.relativeToTimestamp period;period]};

// Remove a previously added timer.
// @param tid Timer handle returned by one of the addXXTimer functions.
.timer.removeTimer:{[tid]
    if[not type[tid] in -6 -7h; '`$"Expecting an integer id"];
    delete from `.timer.priv.timers where id=tid;
    };

// Change the frequency of a periodic timer or make a previously one-shot timer periodic.
// @param tid Timer handle returned by one of the addXXTimer functions.
// @param period The new timer period (time or timespan)
.timer.adjustPeriodicFrequency:{[tid;newperiod]
    if[not type[tid] in -6 -7h; '`$"Expecting an integer id"];
    if[not tid in exec id from .timer.priv.timers; '`$"invalid timer ID"];
    .timer.priv.timers[tid;`period]:.timer.priv.toTimespan newperiod;
    };

// Change the catch up mode of a periodic timer.
// @param tid Timer handle returned by one of the addXXTimer functions.
// @param mode One of the valid values for [[.timer.defaultCatchUpMode]].
.timer.setCatchUpMode:{[tid;mode]
    if[not type[tid] in -6 -7h; '`$"Expecting an integer id"];
    if[not type[mode]=-11h; '`$"Expecting a symbol mode"];
    if[not mode in .timer.priv.validCatchUpModes; '`$"mode must be one of ",.Q.s1 .timer.priv.validCatchUpModes];
    if[not tid in exec id from .timer.priv.timers; '`$"invalid timer ID"];
    .timer.priv.timers[tid;`catchUpMode]:mode;
    };

{   //the "main" function
    restoreOld:0b;
    if[not ()~key `.z.ts;
        if[()~key `.timer.priv.oldZts; //don't overwrite if this script is reloaded
            period:system"t";
            restoreOld:period>0;    //if period=0, timer is disabled so it shouldn't run
        ];
    ];
    if[restoreOld;
        .timer.priv.oldZts:.z.ts;
    ];
    //invokes expired timers, reschedules periodic timers
    //and resets \t for next expiration
    .z.ts:{
        now:.z.p;
        toRun:`when xasc select from .timer.priv.timers where when<=now;
        .timer.priv.runCallback each 0!toRun;
        .timer.priv.setSystemT[];};
    if[restoreOld;
        .timer.addPeriodicTimer[.timer.priv.oldZts;period];
    ];
    }[];