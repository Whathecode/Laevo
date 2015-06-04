using System;
using System.Collections.Generic;
using System.Diagnostics;
using Laevo.Model;


namespace Laevo.Peer.Clouds.ActivityCloud
{
    public class ActivityChannelProxy : AbstractProxy<IActivityCloud>, IActivityCloud
    {
        public void RequestSync()
        {
            foreach (var c in GetChannels())
            {
                try
                {
                    c.Value.RequestSync();
                }
                catch (Exception e)
                {
                    RemoveChannel(c.Key);
                    Debug.WriteLine(e);
                }
            }
        }

        public void SendStateTable(Dictionary<Guid, DateTime> activityStates, Guid sender)
        {
            foreach (var c in GetChannels())
            {
                try
                {
                    c.Value.SendStateTable(activityStates, sender);
                }
                catch (Exception e)
                {
                    RemoveChannel(c.Key);
                    Debug.WriteLine(e);
                }
            }
        }

        public void RequestActivity( Guid activityId, Guid sender, Guid reciever )
        {
            try
            {
                GetChannel( reciever ).RequestActivity(activityId, sender, reciever);
            }
            catch (KeyNotFoundException)
            {

            }
            catch (Exception e)
            {
                RemoveChannel(reciever);
                Debug.WriteLine(e);
            }
        }

        public void SendActivity(Activity activity, Guid sender, Guid reciever)
        {
            try
            {
                GetChannel(reciever).SendActivity(activity, sender, reciever);
            }
            catch ( KeyNotFoundException  )
            {
                
            }
            catch (Exception e)
            {
                RemoveChannel(reciever);
                Debug.WriteLine(e);
            }
        }

        public void BroadcastActivity( Activity activity )
        {
            foreach (var c in GetChannels())
            {
                try
                {
                    c.Value.BroadcastActivity(activity);
                }
                catch (Exception e)
                {
                    RemoveChannel(c.Key);
                    Debug.WriteLine(e);
                }
            }
        }
    }
}
