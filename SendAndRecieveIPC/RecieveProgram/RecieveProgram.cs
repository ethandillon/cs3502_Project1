using System;
using System.IO;
using System.IO.Pipes;

namespace ReceiverProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting for input from Sender program...");

            try
            {
                while (true)
                {
                    // Create a pipe server that takes in input
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("wordpipe", PipeDirection.In))
                    {
                        // Wait for the client to connect
                        Console.WriteLine("\nWaiting for Sender program to connect...");
                        pipeServer.WaitForConnection();
                        Console.WriteLine("Sender program connected!");

                        // Create a StreamReader to read from the pipe
                        using (StreamReader sr = new StreamReader(pipeServer))
                        {
                            // First read the count of words
                            string countStr = sr.ReadLine();
                            if (int.TryParse(countStr, out int wordCount))
                            {
                                Console.WriteLine($"Expecting to receive {wordCount} words");
                                
                                // Print each word out 
                                for (int i = 0; i < wordCount; i++)
                                {
                                    string receivedWord = sr.ReadLine();
                                    Console.WriteLine($"Received word {i+1}/{wordCount}: \"{receivedWord}\"");
                                }
                                
                                Console.WriteLine("All words received successfully!");
                            }
                            else
                            {
                                Console.WriteLine("Error: Failed to parse word count");
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error: {e.Message}");
            }
        }
    }
}