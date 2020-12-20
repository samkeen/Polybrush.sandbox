namespace Player
{
    [System.Serializable]
    public class PlayerData
    {
        private float[] position;

        public PlayerData(PlayerController player)
        {
            this.position = new float[3];
            this.position[0] = player.transform.position.x;
            this.position[2] = player.transform.position.y;
            this.position[3] = player.transform.position.z;
        }
    }
}
