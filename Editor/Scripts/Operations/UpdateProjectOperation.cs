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
		private FetchOperation<List<Container>> _requestFetchProjectContainers;
		private float _updateProjectProgress = 40f;
		private float _updateContainersProgress = 40f;
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
			var updateProjectContainersTask = UpdateProjectContainers();
			List<Task> tasks = new()
			{
				updateProjectInfoTask,
				updateProjectContainersTask
			};
			await Task.WhenAll(tasks);
			
			FinishOperation(_requestFetchProjectInfo.IsFinishedSuccessfully && 
			                _requestFetchProjectContainers.IsFinishedSuccessfully);
		}
		
		private async Task<bool> UpdateProjectInfo()
		{
			_requestFetchProjectInfo = OperationsController.RequestFetchProjectInfo("projects/info", _projectSo.ApiKey);
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
		
		private async Task<bool> UpdateProjectContainers()
		{
			_requestFetchProjectContainers = OperationsController.RequestFetchProjectContainers("containers", _projectSo.ApiKey);
			while (!_requestFetchProjectContainers.IsFinished)
			{
				await Task.Yield();
				
				if (cancellationToken.IsCancellationRequested)
				{
					return false;	
				}
			}
			
			if(_requestFetchProjectContainers.IsFinishedSuccessfully)
			{
				_projectSo.SyncContainersInfo(_requestFetchProjectContainers.ResponseData);
				UpdateProgress(progress + _updateContainersProgress);
			}
			else
			{
				Logger.LogError($"Failed to fetch containers for {_projectSo.name}", _projectSo);
			}
			
			return _requestFetchProjectContainers.IsFinishedSuccessfully;
		}
	}
}