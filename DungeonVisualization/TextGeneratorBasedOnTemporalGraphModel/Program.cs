
using System.Diagnostics;

namespace TextGeneratorBasedOnTemporalGraphModel
{
    class Program
    {

        static void Main(string[] args)
        {
            //Data.sm.Do();

            Data_SpaceStation.smm.Do();

            Process photoViewer = new Process();
            photoViewer.StartInfo.FileName = @"C:\Programs\PaintNet\paintdotnet.exe";
            photoViewer.StartInfo.Arguments = @"D:\Sperasoft_Research_Works\GeneratorPrototype\DungeonVisualization\TextGeneratorBasedOnTemporalGraphModel\bin\Debug\net6.0\layout.png";
            photoViewer.Start();
        }

    }
}