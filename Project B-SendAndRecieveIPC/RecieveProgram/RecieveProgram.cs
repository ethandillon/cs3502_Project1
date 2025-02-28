using System;
using System.IO;
using System.IO.Pipes;

namespace ReceiverProgram
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Waiting for input from Send program...");

            try
            {
                while (true)
                {
                    // Create a pipe server that takes in input
                    using (NamedPipeServerStream pipeServer = new NamedPipeServerStream("wordpipe", PipeDirection.In))
                    {
                        // Wait for a client to connect
                        Console.WriteLine("\nWaiting for Sender program to connect...");
                        pipeServer.WaitForConnection();
                        Console.WriteLine("Sender program connected!");

                        try
                        {
                            // Create a StreamReader to read from the pipe
                            using (StreamReader sr = new StreamReader(pipeServer))
                            {
                                // First read the count of words
                                string countStr = sr.ReadLine();
                                if (int.TryParse(countStr, out int wordCount))
                                {
                                    Console.WriteLine($"Expecting to receive {wordCount} words");
                                    
                                    // Read each word
                                    for (int i = 0; i < wordCount; i++)
                                    {
                                        try
                                        {
                                            string receivedWord = sr.ReadLine();
                                            
                                            // Check if we got a null (pipe closed)
                                            if (receivedWord == null)
                                            {
                                                throw new IOException("Pipe was closed unexpectedly");
                                            }
                                            //if not null then keep printing each word
                                            Console.WriteLine($"Received word {i+1}/{wordCount}: \"{receivedWord}\"");
                                        }
                                        catch (IOException e)
                                        {
                                            //prints how many words it printed out before it failed
                                            Console.WriteLine($"ERROR during word reception: {e.Message}");
                                            Console.WriteLine($"Successfully received {i} out of {wordCount} words before pipe broke");
                                            break;
                                        }
                                    }
                                    
                                }
                                else
                                {
                                    Console.WriteLine("Error: Failed to parse word count");
                                }
                            }
                        }
                        catch (IOException e)
                        {
                            Console.WriteLine($"Pipe error: {e.Message}");
                            Console.WriteLine("Pipe broken - waiting for new connection...");
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Critical error: {e.Message}");
                Console.WriteLine($"Exception type: {e.GetType().Name}");
                if (e.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                }
            }
            
            Console.WriteLine("Receiver program terminated.");
        }
    }
}