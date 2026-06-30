namespace TarimDonusum.Servisler
{
    public interface IMailServisi
    {
        Task<string> MailAtAsync(
            string kimden,
            string kime,
            string konu,
            string bilgi,
            bool html,
            bool replyVar);
    }
}
