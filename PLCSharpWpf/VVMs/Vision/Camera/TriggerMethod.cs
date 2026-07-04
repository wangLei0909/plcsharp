namespace PLCSharp.VVMs.Vision.Camera
{
    public enum TriggerMethod
    {
        // ch:触发源选择: | en:Trigger source select: 
        //           0 - Line0;
        //           1 - Line1;
        //           2 - Line2;
        //           3 - Line3;
        //           4 - Counter;
        //           7 - Software;

        Line0 = 0,
        Line1 = 1,
        Line2 = 2,
        Line3 = 3,
        Counter = 4,
        Software = 7,

    }
}
