namespace KitchenProject.Classes
{
	public class UserSessionTracker
	{
		private int _activeUsers;

		public int ActiveUsers
		{
			get { return _activeUsers; }
		}

		public void Increment()
		{
			_activeUsers++;
		}

		public void Decrement()
		{
			if (_activeUsers > 0)
			{
				_activeUsers--;
			}
		}

	}
}
