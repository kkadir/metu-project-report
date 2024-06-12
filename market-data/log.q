.log.priv.levels:`fatal`error`warning`notice`info`debug;
.log.priv.handle:-1;
.log.priv.level:.log.priv.levels?`info;
.log.priv.stringify:{ssr[;"¬";" "]ssr[;"\n";"\n "]ssr[$[10=type x;x;97<type x;"\n",.Q.s x;0<=type x;"¬"sv .z.s@'x;string x];"\n¬";"\n"]};
.log.priv.build:{.log.priv.stringify (upper .log.priv.levels x;y)};
.log.priv.write:{if[x<=.log.priv.level;.log.priv.handle .log.priv.build[x;y]];};
.log.set:{.log.priv.level:.log.priv.levels?x;};
{(` sv`.log,x)set .log.priv.write[.log.priv.levels?x;];}'[.log.priv.levels];