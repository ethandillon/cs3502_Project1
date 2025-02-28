using System;
using System.IO;
using System.IO.Pipes;

namespace SenderProgram
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("This program will split input text into words and send them to the Receiver program");
            Console.WriteLine("Enter text to split and send");
            Console.WriteLine("Use 'break' as a word in your input to simulate pipe breakage");

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

                            // Send each word with a small delay for demonstration purposes
                            for (int i = 0; i < words.Length; i++)
                            {
                                string word = words[i];
                                
                                // If the word is "break", simulate pipe breakage
                                if (word.ToLower() == "break")
                                {
                                    Console.WriteLine("SIMULATING PIPE BREAKAGE!");
                                    pipeClient.Close(); // Forcibly close the pipe
                                    throw new IOException("Pipe was broken (simulated)");
                                }
                                
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
                    Console.WriteLine($"ERROR: {e.Message}");
                    Console.WriteLine($"Exception type: {e.GetType().Name}");
                    if (e.InnerException != null)
                    {
                        Console.WriteLine($"Inner exception: {e.InnerException.Message}");
                    }
                    Console.WriteLine("Make sure the Receiver program is running before sending data.");
                }
            }

            Console.WriteLine("Sender program terminated.");
        }
    }
}