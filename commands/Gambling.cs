using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.CommandsNext;
using System.Threading.Tasks;
using System;
using System.Linq;
using System.Collections.Generic;
using DSharpPlus.Interactivity.Extensions;
using System.IO;
using DSharpPlus.Entities;

namespace VegasVibes.commands
{
    public class Card
    {
        public string Name { get; set; }
        public string Suit { get; set; }
        public int Value { get; set; }

        public string GetImagePath()
        {
            if (string.IsNullOrEmpty(Name) || string.IsNullOrEmpty(Suit))
            {
                throw new InvalidOperationException($"Card must have valid Name and Suit. Current values: Name = {Name}, Suit = {Suit}");
            }

            string baseUrl = "https://raw.githubusercontent.com/AthertonH/images/refs/heads/main/";
            string fileName = $"{Name.ToLower()}_of_{Suit.ToLower()}.png";
            return $"{baseUrl}{fileName}";
        }
    }

    // Deck class to manage a deck of cards
    public class Deck
    {
        private List<Card> Cards { get; set; }
        private Random Random { get; set; } = new Random();

        public Deck()
        {
            Cards = new List<Card>();
            string[] ranks = { "2", "3", "4", "5", "6", "7", "8", "9", "10", "Jack", "Queen", "King", "Ace" };
            string[] suits = { "Hearts", "Diamonds", "Clubs", "Spades" };

            foreach (var suit in suits)
            {
                foreach (var rank in ranks)
                {
                    int value;
                    if (rank == "Jack" || rank == "Queen" || rank == "King")
                    {
                        value = 10;
                    }
                    else if (rank == "Ace")
                    {
                        value = 11; // Initially treat Ace as 11
                    }
                    else
                    {
                        value = int.Parse(rank);
                    }

                    Cards.Add(new Card { Name = rank, Suit = suit, Value = value });
                }
            }
        }

        public Card DrawCard()
        {
            if (Cards.Count == 0)
            {
                throw new InvalidOperationException("The deck is empty!");
            }

            int index = Random.Next(Cards.Count);
            Card drawnCard = Cards[index];
            Cards.RemoveAt(index);
            return drawnCard;
        }
    }

    // Hand class to represent a player's or dealer's hand
    public class Hand
    {
        public List<Card> Cards { get; set; } = new List<Card>();

        public int GetHandValue()
        {
            int value = Cards.Sum(card => card.Value);
            int aceCount = Cards.Count(card => card.Name == "Ace");

            // Adjust for Aces if the total value exceeds 21
            while (value > 21 && aceCount > 0)
            {
                value -= 10;
                aceCount--;
            }

            return value;
        }

        public bool IsBust() => GetHandValue() > 21;
    }

    // Gambling class for commands
    internal class Gambling : BaseCommandModule
    {
        public string GetCardImagePath(Card card)
        {
            string basePath = "images/cards/";
            string fileName = $"{card.Name.ToLower()}_of_{card.Suit.ToLower()}.png";
            return Path.Combine(basePath, fileName);
        }

        public static Balances BalancesManager { get; set; } // Static property to hold BalancesManager

        // Command to display the user's balance
        [Command("balance")]
        public async Task Balance(CommandContext ctx)
        {
            int balance = BalancesManager.GetBalance(ctx.User.Id);
            await ctx.Channel.SendMessageAsync($"{ctx.User.Username}, your balance is ${balance}.");
        }

        // Command to roll a random number
        [Command("roll")]
        public async Task Roll(CommandContext ctx, int upperBound = 100)
        {
            Random random = new Random();
            int randomNumber = random.Next(1, upperBound + 1);
            await ctx.Channel.SendMessageAsync($"{ctx.User.Username} rolls {randomNumber} (1-{upperBound})");
        }

        /*  
         * BLACK JACK TASKS 
         */
        private async Task DisplayHand(CommandContext ctx, string title, object handOrCard)
        {
            if (handOrCard is Card card) // Single card
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = title,
                    Description = $"{card.Name} of {card.Suit}",
                    ImageUrl = card.GetImagePath(),
                    Color = DiscordColor.Azure
                };

                await ctx.Channel.SendMessageAsync(embed: embed);
            }
            else if (handOrCard is Hand hand) // Full hand
            {
                var embed = new DiscordEmbedBuilder
                {
                    Title = title,
                    Description = $"Cards: {string.Join(", ", hand.Cards.Select(c => $"{c.Name} of {c.Suit}"))}\nValue: {hand.GetHandValue()}",
                    ImageUrl = hand.Cards.First().GetImagePath(), // Show one image
                    Color = DiscordColor.Azure
                };

                await ctx.Channel.SendMessageAsync(embed: embed);
            }
            else
            {
                throw new ArgumentException("Invalid type passed to DisplayHand. Expected Card or Hand.");
            }
        }

        private async Task DisplayHiddenDealerHand(CommandContext ctx, Hand dealerHand)
        {
            string firstCard = dealerHand.Cards[0].Name;
            await ctx.Channel.SendMessageAsync($"Dealer's hand: {firstCard}, [Hidden]");
        }

        [Command("blackjack")]
        public async Task Blackjack(CommandContext ctx, int wager = 100)
        {
            // Ensure Interactivity is registered
            var interactivity = ctx.Client.GetInteractivity();
            if (interactivity == null)
            {
                await ctx.Channel.SendMessageAsync("Interactivity is not enabled. Please contact the administrator.");
                return;
            }

            // Check if user has enough balance
            if (!BalancesManager.UserBalances.ContainsKey(ctx.User.Id))
            {
                BalancesManager.UserBalances[ctx.User.Id] = GlobalVariables.InitialBalance;
            }

            int balance = BalancesManager.GetBalance(ctx.User.Id);

            if (wager > balance)
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Username}, you don't have enough money to wager ${wager}. Your balance is ${balance}.");
                return;
            }

            // Initialize deck and hands
            Deck deck = new Deck();
            Hand playerHand = new Hand();
            Hand dealerHand = new Hand();

            // Deal initial cards
            playerHand.Cards.Add(deck.DrawCard());
            playerHand.Cards.Add(deck.DrawCard());
            dealerHand.Cards.Add(deck.DrawCard());
            dealerHand.Cards.Add(deck.DrawCard());

            // Display initial hands
            await DisplayHand(ctx, $"{ctx.User.Username}'s Hand", playerHand);
            await DisplayHand(ctx, $"{ctx.User.Username}'s Hand", playerHand.Cards[1]);
            await DisplayHiddenDealerHand(ctx, dealerHand);

            // Player's turn
            while (true)
            {
                await ctx.Channel.SendMessageAsync($"{ctx.User.Username}, do you want to `hit` or `stand`?");
                var response = await interactivity.WaitForMessageAsync(
                    x => x.Author.Id == ctx.User.Id && (x.Content.ToLower() == "hit" || x.Content.ToLower() == "stand"),
                    TimeSpan.FromSeconds(30));

                if (response.TimedOut)
                {
                    await ctx.Channel.SendMessageAsync($"{ctx.User.Username}, you took too long. Standing automatically.");
                    break;
                }

                if (response.Result.Content.ToLower() == "hit")
                {
                    playerHand.Cards.Add(deck.DrawCard());
                    await DisplayHand(ctx, $"{ctx.User.Username}'s Hand", playerHand);

                    if (playerHand.IsBust())
                    {
                        await ctx.Channel.SendMessageAsync($"{ctx.User.Username} busted! Dealer wins.");
                        balance -= wager;
                        await BalancesManager.UpdateBalanceAsync(ctx.User.Id, balance);
                        return;
                    }
                }
                else
                {
                    break;
                }
            }

            // Dealer's turn
            await DisplayHand(ctx, "Dealer's Hand", dealerHand);

            while (dealerHand.GetHandValue() < 17)
            {
                dealerHand.Cards.Add(deck.DrawCard());
                await DisplayHand(ctx, "Dealer's Hand", dealerHand);
            }

            // Determine winner
            if (dealerHand.IsBust())
            {
                await ctx.Channel.SendMessageAsync("Dealer busted! You win!");
                balance += wager;
            }
            else if (dealerHand.GetHandValue() > playerHand.GetHandValue())
            {
                await ctx.Channel.SendMessageAsync("Dealer wins.");
                balance -= wager;
            }
            else if (dealerHand.GetHandValue() < playerHand.GetHandValue())
            {
                await ctx.Channel.SendMessageAsync("You win!");
                balance += wager;
            }
            else
            {
                await ctx.Channel.SendMessageAsync("It's a tie!");
            }

            // Update balance
            await BalancesManager.UpdateBalanceAsync(ctx.User.Id, balance);
        }
    }
}
