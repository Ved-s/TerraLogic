using System;

namespace TerraLogic
{
#if WINDOWS || XBOX
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        static void Main(string[] args)
        {
            using (TerraLogic game = new TerraLogic())
            {
                game.Run();
            }
        }
    }
#endif
}

