using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Player;
using UnityEngine;

namespace Game
{
    public static class SaveSystem
    {
        public static void SavePlayer(PlayerController player)
        {
            var formatter = new BinaryFormatter();
            var path =  $"{Application.persistentDataPath}/gather.player.save";
            var stream = new FileStream(path, FileMode.Create);
            var data = new PlayerData(player);
            formatter.Serialize(stream, data);
            stream.Close();
        }

        public static PlayerData LoadPlayer()
        {
            var path =  $"{Application.persistentDataPath}/gather.player.save";
            if (File.Exists(path))
            {
                var formatter = new BinaryFormatter();
                var stream = new FileStream(path, FileMode.Open);
                var data = formatter.Deserialize(stream) as PlayerData;
                stream.Close();
                return data;
            }
            else
            {
                Debug.LogError($"Save file not found: {path}");
                return null;
            }
        }
    }
}
