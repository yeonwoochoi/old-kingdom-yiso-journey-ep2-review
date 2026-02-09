namespace ServerShared.DTOs.Rank {
    public class RankResponse {
        public string Username { get; set; } = string.Empty;
        public int Score { get; set; }
        public int Rank { get; set; }
    }
}