using System;
using System.Diagnostics;
using Laevo.Model;


namespace Laevo.Peer.Clouds.UserCloud
{
    class UserChannelProxy : AbstractProxy<IUserCloud>, IUserCloud
    {
        public void Invite( User user, Activity activity )
        {
            foreach ( var c in GetChannels() )
            {
                try
                {
                    c.Value.Invite( user, activity );
                }
                catch ( Exception e )
                {
                    RemoveChannel( c.Key );
                    Debug.WriteLine( e );
                }
            }
        }
    }
}