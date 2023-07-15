using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using FxEvents;
using FxEvents.Shared.EventSubsystem;
using GlobalClasses;
using Newtonsoft.Json;
using static CitizenFX.Core.Native.API;

namespace Retro_Multichar_cl
{
    public class Main : BaseScript
    {

        bool playerSpawned = false;
       public Main()
        {
            EventDispatcher.Initalize("pineapple_rpc_in", "orange_rpc_out", "banana_sig");
            EventHandlers["playerSpawned"] += new Action(Spawned);
            Debug.WriteLine("script loaded");

            RegisterNuiCallbackType("submitchar");
            EventHandlers["__cfx_nui:submitchar"] += new Action<IDictionary<string, object>, CallbackDelegate>((data, cb) =>
            { 
                CreateCharacter(data, cb);
            });

            RegisterNuiCallbackType("charselected");
            EventHandlers["__cfx_nui:charselected"] += new Action<IDictionary<string, object>, CallbackDelegate>((data, cb) =>
            {
                CharSelected(data, cb);
            });
        }
        void Spawned()
        {
            if (!playerSpawned)
            {
                playerSpawned = true;
                OnPlayerSpawned();
            }
        }
        public async void OnPlayerSpawned()
        {
            Debug.WriteLine("player spawned");
            int playerPedID = Game.Player.Handle;
            StartPlayerTeleport(playerPedID, 779.8549f, 1175.0345f, 344.1739f, 340.5996f, false, false, false);
            FreezeEntityPosition(playerPedID, true);
            SetEntityInvincible(playerPedID, true);
            NetworkSetEntityInvisibleToNetwork(playerPedID, true);

            List<Character> chars = await EventDispatcher.Get<List<Character>>("rc-multichar:getCharacters");

            Debug.WriteLine("Recieved Character Selection event on client side");
            foreach (Character character in chars)
            {
                NUIEvent nuiEvent = new NUIEvent
                {
                    Name = "char_data",
                    Data = "{\"has_char" + character.Slot + "\": \"true\", \"char_name\":\"" + character.FirstName + " " + character.LastName + "\"}"
                };

                Debug.WriteLine("Sending character info for char " + character.Slot);
                SendNuiMessage(JsonConvert.SerializeObject(nuiEvent));
            }
            SetNuiFocus(true, true);
        }
        void CreateCharacter(IDictionary<string, object> data, CallbackDelegate cb)
        {
            #region jsonstuff
            if (!data.TryGetValue("first_name", out var firstName))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #1 failed!");
                return;
            }
            if (!data.TryGetValue("last_name", out var lastName))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #2 failed!");
                return;
            }
            if (!data.TryGetValue("dob", out var dob))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #3 failed!");
                return;
            }
            if (!data.TryGetValue("gender", out var gender))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #4 failed!");
                return;
            }
            if (!data.TryGetValue("slot", out var slot))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #4 failed!");
                return;
            }
            #endregion
            Character character = new Character()
            {
                CitizenID = slot.ToString(),
                FirstName = firstName.ToString(),
                LastName = lastName.ToString(),
                DateOfBirth = dob.ToString(),
                Gender = gender.ToString()
            };
            SetNuiFocus(false, false);
            int playerPedID = Game.Player.Handle;
            FreezeEntityPosition(playerPedID, false);
            SetEntityInvincible(playerPedID, false);
            NetworkSetEntityInvisibleToNetwork(playerPedID, false);
            EventDispatcher.Send("rc-multichar:createCharacter", character);
        }
        async void CharSelected(IDictionary<string, object> data, CallbackDelegate cb)
        {
            if (!data.TryGetValue("cid", out var cid))
            {
                cb(new
                {
                    error = "Item ID not specified!"
                });

                Debug.WriteLine("Callback #1 failed!");
                return;
            }
            SetNuiFocus(false, false);
            int playerPedID = Game.Player.Handle;
            FreezeEntityPosition(playerPedID, false);
            SetEntityInvincible(playerPedID, false);
            NetworkSetEntityInvisibleToNetwork(playerPedID, false);
            Debug.WriteLine("Trying to get information about the selected character");
            Character character = await EventDispatcher.Get<Character>("rc-multichar:getSelectedCharacter", cid.ToString());
            LoadCharacterAppearance(character);
        }

        async void LoadCharacterAppearance(Character character)
        {
            try
            {
                List<string> appearance = await EventDispatcher.Get<List<string>>("rc-multichar:getCharacterAppearance", character.CitizenID);

                string ped = appearance[0].ToString();
                string headblend = appearance[1].ToString();
                string facialfeatures = appearance[2].ToString();
                string hair = appearance[3].ToString();
                string beardmakeup = appearance[4].ToString();
                string tattoos = appearance[5].ToString();

                List<string> clothes = await EventDispatcher.Get<List<string>>("rc-multichar:getCharacterClothing", character.CitizenID, "default");

                string clothing = clothes[0].ToString();
                string props = clothes[1].ToString();

                TriggerEvent("rc-multichar:setAppearance", ped, headblend, facialfeatures, hair, beardmakeup, tattoos);
                TriggerEvent("rc-multichar:setClothing", clothing, props);
            }
            catch(Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }
    }
}