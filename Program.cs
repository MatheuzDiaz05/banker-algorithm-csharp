using System;
using System.Threading;

class BankerAlgorithm
{
    private const int NumberOfCustomers = 5;
    private const int NumberOfResources = 3;
    private const int RequestCycles = 5;

    private static readonly int[] Available = new int[NumberOfResources];
    private static readonly int[,] Maximum = new int[NumberOfCustomers, NumberOfResources];
    private static readonly int[,] Allocation = new int[NumberOfCustomers, NumberOfResources];
    private static readonly int[,] Need = new int[NumberOfCustomers, NumberOfResources];
    private static readonly object StateLock = new object();
    private static readonly Random Random = new Random();

    static void Main(string[] args)
    {
        if (args.Length != NumberOfResources)
        {
            Console.WriteLine("Uso: dotnet run -- <r1> <r2> <r3>");
            return;
        }

        for (int i = 0; i < NumberOfResources; i++)
        {
            if (!int.TryParse(args[i], out Available[i]) || Available[i] < 0)
            {
                Console.WriteLine("Cada recurso deve ser um número inteiro não negativo.");
                return;
            }
        }

        InitializeState();
        PrintState("Estado inicial");

        var threads = new Thread[NumberOfCustomers];
        for (int i = 0; i < NumberOfCustomers; i++)
        {
            int customerId = i;
            threads[i] = new Thread(() => CustomerRoutine(customerId));
            threads[i].Start();
        }

        for (int i = 0; i < NumberOfCustomers; i++)
        {
            threads[i].Join();
        }

        PrintState("Estado final");
        Console.WriteLine("Execução concluída.");
    }

    private static void InitializeState()
    {
        lock (StateLock)
        {
            for (int i = 0; i < NumberOfCustomers; i++)
            {
                for (int j = 0; j < NumberOfResources; j++)
                {
                    Maximum[i, j] = Random.Next(Available[j] + 1);
                    Allocation[i, j] = 0;
                    Need[i, j] = Maximum[i, j];
                }
            }
        }
    }

    private static void CustomerRoutine(int customerId)
    {
        for (int cycle = 0; cycle < RequestCycles; cycle++)
        {
            int totalNeed;
            lock (StateLock)
            {
                totalNeed = 0;
                for (int j = 0; j < NumberOfResources; j++)
                {
                    totalNeed += Need[customerId, j];
                }
            }

            if (totalNeed == 0)
            {
                break;
            }

            int[] request = new int[NumberOfResources];
            bool allZero;

            do
            {
                allZero = true;
                lock (StateLock)
                {
                    for (int j = 0; j < NumberOfResources; j++)
                    {
                        request[j] = Need[customerId, j] > 0 ? Random.Next(Need[customerId, j] + 1) : 0;
                        if (request[j] > 0)
                        {
                            allZero = false;
                        }
                    }
                }
            } while (allZero);

            if (RequestResources(customerId, request))
            {
                Console.WriteLine($"Cliente {customerId} conseguiu recursos: [{string.Join(", ", request)}]");
                Thread.Sleep(1000);
                ReleaseResources(customerId, request);
                Console.WriteLine($"Cliente {customerId} liberou recursos: [{string.Join(", ", request)}]");
            }
            else
            {
                Console.WriteLine($"Cliente {customerId} teve pedido negado: [{string.Join(", ", request)}]");
            }

            Thread.Sleep(1000);
        }
    }

    private static bool RequestResources(int customerId, int[] request)
    {
        lock (StateLock)
        {
            for (int j = 0; j < NumberOfResources; j++)
            {
                if (request[j] > Need[customerId, j] || request[j] > Available[j])
                {
                    return false;
                }
            }

            for (int j = 0; j < NumberOfResources; j++)
            {
                Available[j] -= request[j];
                Allocation[customerId, j] += request[j];
                Need[customerId, j] -= request[j];
            }

            if (!IsSafe())
            {
                for (int j = 0; j < NumberOfResources; j++)
                {
                    Available[j] += request[j];
                    Allocation[customerId, j] -= request[j];
                    Need[customerId, j] += request[j];
                }
                return false;
            }

            return true;
        }
    }

    private static void ReleaseResources(int customerId, int[] release)
    {
        lock (StateLock)
        {
            for (int j = 0; j < NumberOfResources; j++)
            {
                Available[j] += release[j];
                Allocation[customerId, j] -= release[j];
                Need[customerId, j] += release[j];
            }
        }
    }

    private static bool IsSafe()
    {
        int[] work = new int[NumberOfResources];
        bool[] finish = new bool[NumberOfCustomers];

        for (int j = 0; j < NumberOfResources; j++)
        {
            work[j] = Available[j];
        }

        while (true)
        {
            bool found = false;

            for (int i = 0; i < NumberOfCustomers; i++)
            {
                if (!finish[i])
                {
                    int j;
                    for (j = 0; j < NumberOfResources; j++)
                    {
                        if (Need[i, j] > work[j])
                        {
                            break;
                        }
                    }

                    if (j == NumberOfResources)
                    {
                        for (int k = 0; k < NumberOfResources; k++)
                        {
                            work[k] += Allocation[i, k];
                        }
                        finish[i] = true;
                        found = true;
                    }
                }
            }

            if (!found)
            {
                break;
            }
        }

        foreach (bool done in finish)
        {
            if (!done)
            {
                return false;
            }
        }

        return true;
    }

    private static void PrintState(string title)
    {
        lock (StateLock)
        {
            Console.WriteLine($"\n{title}");
            Console.WriteLine($"Disponível: [{string.Join(", ", Available)}]");
            Console.WriteLine("Máximo:");
            for (int i = 0; i < NumberOfCustomers; i++)
            {
                int[] row = new int[NumberOfResources];
                for (int j = 0; j < NumberOfResources; j++) row[j] = Maximum[i, j];
                Console.WriteLine($"  Cliente {i}: [{string.Join(", ", row)}]");
            }

            Console.WriteLine("Alocado:");
            for (int i = 0; i < NumberOfCustomers; i++)
            {
                int[] row = new int[NumberOfResources];
                for (int j = 0; j < NumberOfResources; j++) row[j] = Allocation[i, j];
                Console.WriteLine($"  Cliente {i}: [{string.Join(", ", row)}]");
            }

            Console.WriteLine("Necessidade:");
            for (int i = 0; i < NumberOfCustomers; i++)
            {
                int[] row = new int[NumberOfResources];
                for (int j = 0; j < NumberOfResources; j++) row[j] = Need[i, j];
                Console.WriteLine($"  Cliente {i}: [{string.Join(", ", row)}]");
            }
        }
    }
}
