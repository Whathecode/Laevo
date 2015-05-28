using System;
using System.Collections.Generic;
using System.Diagnostics;
using Laevo.Model;


namespace Laevo.Peer.Clouds.UserCloud
{
    class UserChannelProxy : IProxy<IUserCloud>, IUserCloud
    {
        public Dictionary<Guid, IUserCloud> Channels { get; set; }

        public UserChannelProxy()
        {
            Channels = new Dictionary<Guid, IUserCloud>();
        }

        public void Invite( User user, Activity activity )
        {
            var remove = new List<Guid>();
            foreach ( var c in Channels )
            {
                try
                {
                    c.Value.Invite( user, activity );
                }
                catch ( Exception e )
                {
                    remove.Add( c.Key );
                    Debug.WriteLine( e );
                }
            }

            foreach ( var guid in remove )
            {
                Channels.Remove( guid );
            }
        }
    }
}