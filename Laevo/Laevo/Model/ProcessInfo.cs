namespace Laevo.Model
{
	public class ProcessInfo
	{
		public int Id { get; private set; }
		public string Name { get; private set; }


		public ProcessInfo( int id, string name )
		{
			Id = id;
			Name = name;
		}
	}
}
