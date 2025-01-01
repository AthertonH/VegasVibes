using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace VegasVibes
{
    public static class GlobalVariables
    {
        public static int InitialBalance = 100000;
    }

    internal class Balances
    {
        // This folder will be located in /bin/Debug/config/userBalances.json. Don't know why, but it works - Atherton
        private static readonly string BalancesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config", "userBalances.json");

        private static void EnsureConfigFolderExists()
        {
            string folderPath = Path.GetDirectoryName(BalancesFilePath);
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
        }

        public Dictionary<ulong, int> UserBalances { get; private set; } = new Dictionary<ulong, int>();

        public async Task LoadBalancesAsync()
        {
            EnsureConfigFolderExists();

            if (File.Exists(BalancesFilePath))
            {
                using (StreamReader sr = new StreamReader(BalancesFilePath))
                {
                    string json = await sr.ReadToEndAsync();
                    UserBalances = JsonConvert.DeserializeObject<Dictionary<ulong, int>>(json) ?? new Dictionary<ulong, int>();
                }
            }
        }

        public async Task SaveBalancesAsync()
        {
            EnsureConfigFolderExists();

            using (StreamWriter sw = new StreamWriter(BalancesFilePath, false))
            {
                string json = JsonConvert.SerializeObject(UserBalances, Formatting.Indented);
                await sw.WriteAsync(json);
            }
        }

        // Get a user's balance
        public int GetBalance(ulong userId)
        {
            if (!UserBalances.ContainsKey(userId))
            {
                UserBalances[userId] = GlobalVariables.InitialBalance; // Default balance
            }

            return UserBalances[userId];
        }

        // Update a user's balance
        public async Task UpdateBalanceAsync(ulong userId, int newBalance)
        {
            UserBalances[userId] = newBalance;
            await SaveBalancesAsync(); // Save balances after updating
        }
    }
}
