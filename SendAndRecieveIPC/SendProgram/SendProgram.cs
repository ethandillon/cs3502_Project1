using System;
using System.IO;
using System.IO.Pipes;

namespace SenderProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {   

            Console.WriteLine("This program will split input text into words and send them to the receiver program");
            Console.WriteLine("Enter text to split and send:");

            while (true)
            {
                //reads in input here
                Console.Write("\nInput: ");
                string input = Console.ReadLine();

                //if the string is empty then it gives an error message
                if (string.IsNullOrWhiteSpace(input))
                {
                    Console.WriteLine("Input cannot be empty. Try again.");
                    continue;
                }

                try
                {
                    // Split the input into words by each space
                    string[] words = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                    
                    Console.WriteLine($"Split into {words.Length} words, now sending to Receiver...");

                    // Create a named pipe client going out to send each word
                    using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", "wordpipe", PipeDirection.Out))
                    {
                        Console.WriteLine("Connecting to Receiver program...");
                        pipeClient.Connect();
                        Console.WriteLine("Connected to Receiver program!");

                        // Create a StreamWriter around the pipe
                        using (StreamWriter sw = new StreamWriter(pipeClient))
                        {
                            sw.AutoFlush = true;

                            // First send the count
                            sw.WriteLine(words.Length);

                            // Send each word with a small delay
                            foreach (string word in words)
                            {
                                sw.WriteLine(word);
                                Console.WriteLine($"Sent: \"{word}\"");
                                //short delay so that each word delivered is visible
                                await Task.Delay(500); 
                            }
                        }
                    }
                    
                    Console.WriteLine("All words sent successfully!");
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Error: {e.Message}");
                    Console.WriteLine("Make sure the Receiver program is running before sending data.");
                }
            }
        }
    }
}