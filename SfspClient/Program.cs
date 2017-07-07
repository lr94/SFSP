namespace SfspClient
{
    class Program
    {
        [System.STAThread]
        public static void Main(string[] args)
        {
            SingleInstanceManager sim = new SingleInstanceManager();
            sim.Run(args);
        }
    }
}
