using BotStuckDiagnostics;
void Check(bool b,string s){if(!b)throw new Exception(s);Console.WriteLine("PASS "+s);}
var a=new IdleTracker(0,new(0,0,0));for(int i=1;i<15;i++)a.Observe(i,new(0,0,i%2==0?0:60));Check(a.Observe(15,new(0,0,0)).Notice==Notice.First&&a.VerticalSpan==60,"in-place jumping is stationary horizontally");Check(a.Observe(16,new(25,0,0)).Notice==Notice.Resumed,"horizontal progress closes episode");
a=new(0,new(0,0,0));for(int i=1;i<=15;i++)a.Observe(i,new(0,0,0));Check(a.Observe(16,new(0,0,97)).Notice==Notice.Resumed,"large vertical progress closes episode");Check(a.Observe(22,new(0,0,97)).Notice==Notice.None&&a.Started==22,"sampling gaps reset history");a.Observe(21,new(0,0,97));Check(a.Started==21,"clock rollback resets history");
