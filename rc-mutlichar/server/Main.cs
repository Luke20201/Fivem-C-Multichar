using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CitizenFX;
using CitizenFX.Core;
using static CitizenFX.Core.Native.API;
using MySql.Data.MySqlClient;
using GlobalClasses;
using FxEvents;
using FxEvents.Shared.EventSubsystem;

namespace Retro_Multichar_sv
{
    public class Main : BaseScript
    {
        public Main()
        {
            EventDispatcher.Initalize("pineapple_rpc_in", "orange_rpc_out", "banana_sig");
            EventHandlers["playerConnecting"] += new Action<Player, string, dynamic, dynamic>(OnPlayerConnecting);
            Debug.WriteLine("script loaded");
            EventDispatcher.Mount("rc-multichar:getCharacters", new Func<Player, Task<List<Character>>>(OnGetCharacters));
            EventDispatcher.Mount("rc-multichar:getSelectedCharacter", new Func<Player, string, Task<Character>>(GetSelectedCharacter));
            EventDispatcher.Mount("rc-multichar:createCharacter", new Action<Player, Character>(CreateCharacter));
            EventHandlers["rc-multichar:saveAppearance"] += new Action<string, string, string, string, string, string, string, string, string>(SaveAppearance);
            EventDispatcher.Mount("rc-multichar:getCharacterAppearance", new Func<string, Task<List<string>>>(GetCharacterApperance));
            EventDispatcher.Mount("rc-multichar:getCharacterClothing", new Func<string, string, Task<List<string>>>(GetCharacterClothing));
        }

        void OnPlayerConnecting([FromSource] Player player, string playerName, dynamic kickReason, dynamic defer)
        {
            defer.defer();
            defer.update("Retrieving user info from the database...");

            // Run identifier check
            if (DB.CheckIfExist("SELECT * FROM `users` WHERE `license` = \"" + player.Identifiers["license"] + "\";"))
            {
                // run a ban check here or sumin
                defer.done();
            }
            else
            {
                DB.Insert("INSERT INTO `users` (license, steamid, discordid)" +
                    " VALUES('" + player.Identifiers["license"] + "', '" + player.Identifiers["steam"] + "','" + player.Identifiers["discord"] + "');");
                defer.done();
            }
        }
        async Task<List<Character>> OnGetCharacters([FromSource]Player player)
        {
            List<object[]> characters = DB.Retrieve("SELECT * FROM `characters` WHERE license = \"" + player.Identifiers["license"] + "\";");
            List<Character> charList = new List<Character>();
            foreach (object[] character in characters)
            {
                Character person = new Character()
                {
                    Slot = character[1].ToString(),
                    CitizenID = character[2].ToString(),
                    FirstName = character[3].ToString(),
                    LastName = character[4].ToString(),
                };
                charList.Add(person);
            }
            Debug.WriteLine("Sending Character Selection event to client side");
            return charList;
        }

        void CreateCharacter([FromSource]Player player, Character character)
        {
            string citizenID = GenerateCitizenID();
            if (DB.CheckIfExist("SELECT * FROM `characters` WHERE `CitizenID` = \"" + citizenID + "\"")) // verify the id isn't used already, no need to keep verifying, 0.001% chance of it being the same id twice.
            {
                citizenID = GenerateCitizenID();
            }

            string query = "INSERT INTO `characters` (license, slot, citizenid, firstname, lastname, dateofbirth, gender) VALUES(" +
                "'" + player.Identifiers["license"] +  "'," +
                "'" + character.CitizenID + "'," + // CitizenID was set temporarily client side as the slot of the character
                "'" + citizenID + "'," +
                "'" + character.FirstName + "'," +
                "'" + character.LastName + "'," +
                "'" + character.DateOfBirth + "'," +
                "'" + character.Gender + "');";
            DB.Insert(query);
            player.TriggerEvent("rc-multichar:createAppearance", citizenID, character.Gender);
        }
        void SaveAppearance(string citizenID, string model, string headBlend, string faceFeatures, string hair, string beardMakeup, string tatoos, string clothes, string props)
        {
            string query = "INSERT INTO `player_appearance` (citizenid, ped, headblend, facialfeatures, hair, beardmakeup, tattoos) VALUES(" +
                "'" + citizenID + "'," +
                "'" + model + "'," +
                "'" + headBlend + "'," +
                "'" + faceFeatures + "'," +
                "'" + hair + "'," +
                "'" + beardMakeup + "'," +
                "'" + tatoos + "');";
            DB.Insert(query);

            query = "INSERT INTO `player_clothing` (citizenid, clothing, props) VALUES(" +
                "'" + citizenID + "'," +
                "'" + clothes + "'," +
                "'" + props + "');";
            DB.Insert(query);
        }

        async Task<Character> GetSelectedCharacter([FromSource] Player player, string cid)
        {
            string query = "SELECT * FROM `characters` WHERE `license` = '" + player.Identifiers["license"] + "' AND `slot` = '" + cid + "';";
            List<object[]> DBPerson = DB.Retrieve(query);
            Character character = new Character();

            foreach (object[] obj in DBPerson)
            {
                character.CitizenID = obj[2].ToString();
                character.FirstName = obj[3].ToString();
                character.LastName = obj[4].ToString();
                character.DateOfBirth = obj[5].ToString();
                character.Job = obj[6].ToString();
                character.BankBalance = obj[7].ToString();
                character.CashBalance = obj[8].ToString();
                character.LastLocation = obj[9].ToString();
                character.Gender = obj[10].ToString();
            }
            return character;
        }

        async Task<List<string>> GetCharacterApperance(string citizenid)
        {
            string query = "SELECT * FROM `player_appearance` WHERE `citizenid` = '" + citizenid + "';";
            List<string> appearance = new List<string>();
        
            foreach (object[] obj in DB.Retrieve(query))
            {
                appearance.Add(obj[1].ToString());
                appearance.Add(obj[2].ToString());
                appearance.Add(obj[3].ToString());
                appearance.Add(obj[4].ToString());
                appearance.Add(obj[5].ToString());
                appearance.Add(obj[6].ToString());
            }
            return appearance;
        }

        async Task<List<string>> GetCharacterClothing(string citizenid, string outfitname)
        {
            string query = "SELECT * FROM `player_clothing` WHERE `citizenid` = '" + citizenid + "' AND `outfitname` = 'default'";
            List<string> clothes = new List<string>();
            foreach (object[] obj in DB.Retrieve(query))
            {
                clothes.Add(obj[2].ToString());
                clothes.Add(obj[3].ToString());
            }
            Debug.WriteLine(clothes[0]);
            return clothes;
        }

        string GenerateCitizenID()
        {
            Random random = new Random();
            string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

            StringBuilder citizenID = new StringBuilder();
            for (int i = 0; i < 10; i++)
            {
                int randomIndex = random.Next(chars.Length);
                citizenID.Append(chars[randomIndex]);
            }
            return citizenID.ToString();
        }
    }
}