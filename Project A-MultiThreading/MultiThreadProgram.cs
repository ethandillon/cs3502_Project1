using System;
using System.Threading;

//three aspects of the bank account class: 
//1. AccountId which is a unique identifier for that bank account
//2. Balance which is the amount of money in the account currently
//3. A mutex/lock object that says wether that specific account is currently being accessed or not
public class BankAccount{
    public int AccountId { get; }
    private decimal balance;
    internal readonly object balanceLock = new object();
    
    public BankAccount(int accountId, decimal initialBalance)
    {
        AccountId = accountId;
        balance = initialBalance;
    }

    public void Deposit(decimal amount)
    {
        lock (balanceLock)
        {
            balance += amount;
        }
    }

    public void Withdraw(decimal amount)
    {
        lock (balanceLock)
        {
            if (balance >= amount) balance -= amount;
        }
    }
    //This method transfers money between two accounts but does it in such a way that the lock is disabled on the account, allowing for a lockout
    public static bool TransferUnsafe(BankAccount fromAccount, BankAccount toAccount, decimal amount, CancellationToken cancellationToken)
{
    Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} attempting to lock Account {fromAccount.AccountId} for ${amount} transfer");
    
    bool lockTaken = false;
    try
    {   
        // Use Monitor.Enter instead of TryEnter to force locking and increase deadlock chance
        Monitor.Enter(fromAccount.balanceLock);
        lockTaken = true;
        
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} successfully locked Account {fromAccount.AccountId}");
        
        // Long sleep to ensure the other thread has time to lock its account, forcing a deadlock
        Thread.Sleep(1000); // 1 second delay
        
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} attempting to lock Account {toAccount.AccountId}");
        
        bool innerLockTaken = false;
        try
        {   
            // Check cancellation before attempting second lock
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} cancelled while holding lock on Account {fromAccount.AccountId}");
                return false;
            }
            
            // Attempt to lock the second account (this is where deadlock will occur)
            Monitor.Enter(toAccount.balanceLock);
            innerLockTaken = true;
            // That means that this thread got to the account before the account had been locked by another thread
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} successfully locked Account {toAccount.AccountId}");
            
            // If the cancellation token says that there has been a cancellation requested to the thread, then the thread will stop safely after its task is done
            if (cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} cancelled after both locks");
                return false;
            }
            
            // If it makes it to this point then that means both accounts 
            // were not locked and it is safe to transfer between the accounts
            fromAccount.balance -= amount;
            toAccount.balance += amount;
            
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} transferred ${amount} from Account {fromAccount.AccountId} to Account {toAccount.AccountId}");
            return true;
        }
        finally
        {
            // Remove inner lock once process is finished
            if (innerLockTaken)
                Monitor.Exit(toAccount.balanceLock);
        }
    }
    finally
    {
        // Remove outer lock once process is finished
        if (lockTaken)
            Monitor.Exit(fromAccount.balanceLock);
    }
}

    public static bool TransferSafe(BankAccount fromAccount, BankAccount toAccount, decimal amount)
    {
        // Always locks accounts in a consistent order to prevent deadlocks
        var firstAccount = fromAccount.AccountId < toAccount.AccountId ? fromAccount : toAccount;
        var secondAccount = fromAccount.AccountId < toAccount.AccountId ? toAccount : fromAccount;
        
        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} safely locking Account {firstAccount.AccountId} first for ${amount} transfer");
        //locking first account first
        lock (firstAccount.balanceLock)
        {
            Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} safely locking Account {secondAccount.AccountId} second for ${amount} transfer");
            //then locking second account
            lock (secondAccount.balanceLock)
            {
                // If we're locking accounts in reverse order fromAccount what we need, we need to swap the operation
                if (firstAccount.AccountId == fromAccount.AccountId)
                {
                    //if there is enough money in the bank account to make the transaction, then the transfer can be completed
                    if (fromAccount.balance >= amount)
                    {
                        fromAccount.balance -= amount;
                        toAccount.balance += amount;
                        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} successfully transferred ${amount} from Account {fromAccount.AccountId} to Account {toAccount.AccountId}");
                        return true;
                    }
                }
                //swaps the opposite way if the accounts are in wrong order
                else
                {
                    if (toAccount.balance >= amount)
                    {
                        toAccount.balance -= amount;
                        fromAccount.balance += amount;
                        Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} successfully transferred ${amount} from Account {toAccount.AccountId} to Account {fromAccount.AccountId}");
                        return true;
                    }
                }
                //if it gets to this point then there was not enough money in the accounts to complete the transaction
                Console.WriteLine($"Thread {Thread.CurrentThread.ManagedThreadId} failed to transfer ${amount} due to insufficient funds");
                return false;
            }
        }
    }
    public decimal GetBalance() => balance;
}

//implementation of the banking system
class BankingSystemDemo {
    //creates two bank accounts and asks the user to enter amounts for each account
    //with neccecary checks to ensure account values are entered correctly
    static List<BankAccount> CreateAccounts()
    {
        {
            var accounts = new List<BankAccount>();
            Console.WriteLine("Bank Account Setup");
            Console.WriteLine("------------------");

            // Account 1
            Console.Write("Enter initial balance for Account 1: $");
            string input1 = Console.ReadLine();
            decimal balance1;

            if (string.IsNullOrEmpty(input1))
            {
                Console.WriteLine("Invalid input. Using default balance of $0 for Account 1.");
                balance1 = 0;
            }
            else if (!decimal.TryParse(input1, out balance1))
            {
                Console.WriteLine("Invalid number. Using default balance of $0 for Account 1.");
                balance1 = 0;
            }
            else if (balance1 < 0)
            {
                Console.WriteLine("Balance cannot be negative. Using default balance of $0 for Account 1.");
                balance1 = 0;
            }

            accounts.Add(new BankAccount(1, balance1));

            // Account 2
            Console.Write("Enter initial balance for Account 2: $");
            string input2 = Console.ReadLine();
            decimal balance2;

            if (string.IsNullOrEmpty(input2))
            {
                Console.WriteLine("Invalid input. Using default balance of $0 for Account 2.");
                balance2 = 0;
            }
            else if (!decimal.TryParse(input2, out balance2))
            {
                Console.WriteLine("Invalid number. Using default balance of $0 for Account 2.");
                balance2 = 0;
            }
            else if (balance2 < 0)
            {
                Console.WriteLine("Balance cannot be negative. Using default balance of $0 for Account 2.");
                balance2 = 0;
            }

            accounts.Add(new BankAccount(2, balance2));

            return accounts;
        }
    }

    

    static void Main(){
        try{
            var accounts = CreateAccounts();
            
            // Phase 1: Basic Thread Operations
            Console.WriteLine("\nPhase 1: Basic Thread Operations");
            Console.WriteLine($"Account {accounts[0].AccountId} starting balance: ${accounts[0].GetBalance()}");
            //creates 10 different threads
            var basicThreads = new List<Thread>();
            for (int i = 0; i < 10; i++)
            {
                var thread = new Thread(() => {
                    //each thread deposits and withdraws from account one 100 times to verify functioning locking mechanics
                    for (int j = 0; j < 100; j++)
                    {
                        accounts[0].Deposit(10);
                        accounts[0].Withdraw(10);
                    }
                });

                basicThreads.Add(thread);
                thread.Start();
            }
            
            foreach (var thread in basicThreads)
            {
                thread.Join();
            }
            
            Console.WriteLine($"Account {accounts[0].AccountId} final balance after basic operations: ${accounts[0].GetBalance()}");
            
            // Phase 2 & 3: Resource Protection & Deadlock Creation
        Console.WriteLine("\nPhase 2: Resource Protection & Phase 3: Deadlock Creation");
        Console.WriteLine("Unsafe Transfer Demonstration:");
        Console.WriteLine($"Account {accounts[0].AccountId} balance: ${accounts[0].GetBalance()}");
        Console.WriteLine($"Account {accounts[1].AccountId} balance: ${accounts[1].GetBalance()}");

        decimal transferAmount1 = 100;
        decimal transferAmount2 = 50;

        Console.WriteLine($"Thread 1 will attempt to transfer ${transferAmount1} fromAccount Account {accounts[0].AccountId} to Account {accounts[1].AccountId}");
        Console.WriteLine($"Thread 2 will attempt to transfer ${transferAmount2} fromAccount Account {accounts[1].AccountId} to Account {accounts[0].AccountId}");


            using (var cancellationTokenSource = new CancellationTokenSource()) 
            {
                //using a token is better then just ending the thread outright because the thread could be in the middle of work and it could corrupt data that way
                //but by using a token, the thread knows it needs to cancel and cancels once it is at a good stopping point
                var token = cancellationTokenSource.Token;
                
                bool thread1Result = false;
                bool thread2Result = false;
                
                //starts two threads that will attempt to transfer onto a locked bank account that the other thread is holding onto
                //and because the threads are waiting for each other to finish, this results in a deadlock
                //becuase they are both waiting for each other to finish before they proceed.
                var thread1 = new Thread(() => 
                {
                    thread1Result = BankAccount.TransferUnsafe(accounts[0], accounts[1], transferAmount1, token);
                });
                
                var thread2 = new Thread(() => 
                {
                    thread2Result = BankAccount.TransferUnsafe(accounts[1], accounts[0], transferAmount2, token);
                });
                
                //start first thread then small delay then next thread, giving thread 1 time to get accounts locked and then waiting
                thread1.Start();
                Thread.Sleep(50);
                thread2.Start();
                
                // Wait for threads to complete or timeout for 3 seconds
                bool threadOneCompleted = thread1.Join(3000); 
                bool threadTwoCompleted = thread2.Join(3000);
                
                //if both threads are not complete then a deadlock has occured
                if (!threadOneCompleted || !threadTwoCompleted)
            {
                Console.WriteLine("\nDEADLOCK DETECTED: One or both threads failed to complete within 3 seconds");
                Console.WriteLine("Cancelling deadlocked transfers...");
                cancellationTokenSource.Cancel();
                
                // Give threads a moment to respond to cancellation
                thread1.Join(1000);
                thread2.Join(1000);
                
                //If both threads are still alive then that means they did not crash, they got deadlocked
                if (thread1.IsAlive || thread2.IsAlive)
                {
                    Console.WriteLine("Warning: Threads still alive after cancellation - true deadlock confirmed");
                }
                
                Console.WriteLine($"Thread 1 result: {thread1Result}");
                Console.WriteLine($"Thread 2 result: {thread2Result}");
            }
            else
            {
                Console.WriteLine("Both threads completed (no deadlock occurred unexpectedly)");
                Console.WriteLine($"Thread 1 result: {thread1Result}");
                Console.WriteLine($"Thread 2 result: {thread2Result}");
            }
        }

        // Show balances after deadlock
        Console.WriteLine($"\nBalances after unsafe transfer attempt:");
        Console.WriteLine($"Account {accounts[0].AccountId}: ${accounts[0].GetBalance()}");
        Console.WriteLine($"Account {accounts[1].AccountId}: ${accounts[1].GetBalance()}");

            
            // Phase 4: Deadlock Resolution
            Console.WriteLine("\nPhase 4: Deadlock Resolution");
            Console.WriteLine("Safe Transfer Demonstration:");
            Console.WriteLine($"Account {accounts[0].AccountId} balance: ${accounts[0].GetBalance()}");
            Console.WriteLine($"Account {accounts[1].AccountId} balance: ${accounts[1].GetBalance()}");
            
            var safeThread1 = new Thread(() => {
                bool result = BankAccount.TransferSafe(accounts[0], accounts[1], transferAmount1);
                Console.WriteLine($"Safe transfer 1 result: {result}");
            });
            
            var safeThread2 = new Thread(() => {
                bool result = BankAccount.TransferSafe(accounts[1], accounts[0], transferAmount2);
                Console.WriteLine($"Safe transfer 2 result: {result}");
            });
            
            safeThread1.Start();
            safeThread2.Start();
            
            // These should complete without deadlock
            safeThread1.Join();
            safeThread2.Join();
            
            Console.WriteLine("\nFinal Balances:");
            Console.WriteLine($"Account {accounts[0].AccountId}: ${accounts[0].GetBalance()}");
            Console.WriteLine($"Account {accounts[1].AccountId}: ${accounts[1].GetBalance()}");
            
            Console.WriteLine("\nDemo completed.");
        }
        catch (Exception e)
        {
            Console.WriteLine($"An error occurred: {e.Message}");
        }
    }
}