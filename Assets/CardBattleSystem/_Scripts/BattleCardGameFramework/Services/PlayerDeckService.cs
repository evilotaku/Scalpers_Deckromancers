using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using csbcgf;

namespace BattleCardGameFramework
{
    public class PlayerDeckService : MonoBehaviour
    {
        public static PlayerDeckService Instance { get; private set; }

        private Dictionary<int, ICard> m_Library = new Dictionary<int, ICard>();
        private Dictionary<string, List<int>> m_Decks = new Dictionary<string, List<int>>();

        public bool IsInitialized { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public async Task InitializeAsync()
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }

            await LoadFromCloudAsync();
            IsInitialized = true;
        }

        public async Task LoadFromCloudAsync()
        {
            try
            {
                var keys = new HashSet<string> { "CardLibrary", "PlayerDecks" };
                var results = await CloudSaveService.Instance.Data.Player.LoadAsync(keys);

                if (results.TryGetValue("CardLibrary", out var libraryItem))
                {
                    m_Library = JsonSerializer.FromJson<Dictionary<int, ICard>>(libraryItem.Value.GetAsString()) ?? new Dictionary<int, ICard>();
                }

                if (results.TryGetValue("PlayerDecks", out var decksItem))
                {
                    m_Decks = JsonSerializer.FromJson<Dictionary<string, List<int>>>(decksItem.Value.GetAsString()) ?? new Dictionary<string, List<int>>();
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to load player data from Cloud Save: {e.Message}");
            }
        }

        public async Task SaveToCloudAsync()
        {
            try
            {
                string libraryJson = JsonSerializer.ToJson(m_Library);
                string decksJson = JsonSerializer.ToJson(m_Decks);

                var data = new Dictionary<string, object>
                {
                    { "CardLibrary", libraryJson },
                    { "PlayerDecks", decksJson }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(data);
                Debug.Log("Player decks and library saved to Cloud Save.");
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to save player data to Cloud Save: {e.Message}");
            }
        }

        public void AddCardToLibrary(ICard card)
        {
            if (card == null) return;
            int id = card.GetType().FullName.GetHashCode();
            if (card is Card concrete) concrete.Id = id;
            m_Library[id] = card;
        }

        public void SaveDeck(string name, List<int> cardIds)
        {
            m_Decks[name] = cardIds;
        }

        public List<int> GetDeck(string name)
        {
            return m_Decks.TryGetValue(name, out var ids) ? ids : null;
        }

        public List<string> GetDeckNames()
        {
            return m_Decks.Keys.ToList();
        }

        public Dictionary<int, ICard> GetLibrary()
        {
            return new Dictionary<int, ICard>(m_Library);
        }
    }
}
