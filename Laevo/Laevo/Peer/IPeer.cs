using Laevo.Model;


namespace Laevo.Peer
{
    public interface IPeer
    {
        User User { get; set; }
        string Cloudname { get; set; }
        void Start();
    }
}
