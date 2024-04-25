using System.Collections.Generic;
using System.Threading.Tasks;
using Lungfetcher.Data;
using Lungfetcher.Editor.Scriptables;
using Lungfetcher.Helper;

namespace Lungfetcher.Editor.Operations
{
	public class UpdateProjectOperation : RequestOperation
	{
		private FetchOperation<Project> _requestFetchProjectInfo;
		private FetchOperation<List<Table>> _requestFetchProjectTables;
		private float _updateProjectProgress = 40f;
		private float _updateTablesProgress = 40f;
		private ProjectSo _projectSo;

		public UpdateProjectOperation(ProjectSo projectSo)
		{
			_projectSo = projectSo;
			GenerateCancellationToken();
			UpdateProject();
		}
		
		private async void UpdateProject()
		{
			var updateProjectInfoTask = UpdateProjectInfo();
			var updateProjectTablesTask = UpdateProjectTables();
			List<Task> tasks = new()
			{
				updateProjectInfoTask,
				updateProjectTablesTask
			};
			await Task.WhenAll(tasks);
			
			FinishOperation(_requestFetchProjectInfo.IsFinishedSuccessfully && 
			                _requestFetchProjectTables.IsFinishedSuccessfully);
		}
		
		private async Task<bool> UpdateProjectInfo()
		{
			_requestFetchProjectInfo = OperationsController.RequestFetchProjectInfo("info", _projectSo.ApiKey);
			while (!_requestFetchProjectInfo.IsFinished)
			{
				await Task.Yield();

				if (cancellationToken.IsCancellationRequested)
				{
					return false;
				}
			}

			if (_requestFetchProjectInfo.IsFinishedSuccessfully)
			{
				_projectSo.SyncProjectInfo(_requestFetchProjectInfo.ResponseData);
				UpdateProgress(progress + _updateProjectProgress);
			}
			else
			{
				Logger.LogError($"Failed to fetch project info for {_projectSo.name}", _projectSo);
			}
				
			
			return _requestFetchProjectInfo.IsFinishedSuccessfully;
		}
		
		private async Task<bool> UpdateProjectTables()
		{
			_requestFetchProjectTables = OperationsController.RequestFetchProjectTables("tables", _projectSo.ApiKey);
			while (!_requestFetchProjectTables.IsFinished)
			{
				await Task.Yield();
				
				if (cancellationToken.IsCancellationRequested)
				{
					return false;	
				}
			}
			
			if(_requestFetchProjectTables.IsFinishedSuccessfully)
			{
				_projectSo.SyncTablesInfo(_requestFetchProjectTables.ResponseData);
				UpdateProgress(progress + _updateTablesProgress);
			}
			else
			{
				Logger.LogError($"Failed to fetch tables for {_projectSo.name}", _projectSo);
			}
			
			return _requestFetchProjectTables.IsFinishedSuccessfully;
		}
	}
}