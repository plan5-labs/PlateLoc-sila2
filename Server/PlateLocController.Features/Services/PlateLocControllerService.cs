using Grpc.Core;
using Grpc.Net.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlateLocCommandExecutor;
using Sila2.Org.Riken.Instruments.Plateloccontroller.V1;
using Sila2.Org.Silastandard;
using SiLA2.Commands;
using SiLA2.Server;
using SiLA2.Server.Utils;
using SiLA2.Utils;
using static Sila2.Org.Riken.Instruments.Plateloccontroller.V1.PlateLocController;

namespace PlateLocController.Features.Services
{
    public class PlateLocControllerService : PlateLocControllerBase
    {
        private readonly SiLA2.Feature _silaFeature;
        private readonly PlateLocCommandExecutorService.PlateLocCommandExecutorServiceClient _executorClient;
        private readonly ILogger _logger;

        // commandManagers
        private IObservableCommandManager<StartCycle_Parameters, StartCycle_Responses> _startCycleCommandManager;
        private IObservableCommandManager<StopCycle_Parameters, StopCycle_Responses> _stopCycleCommandManager;
        private IObservableCommandManager<Reset_Parameters, Reset_Responses> _resetCommandManager;

        private const string COMMAND_IS_RUNNING = "CommandIsRunning";
        private const string NOT_INITIALIZED = "NotInitialized";
        private const string COMMAND_EXECUTION_ABORTED = "CommandExecutionAborted";
        private const string INSTRUMENT_ERROR_OCCURRED = "InstrumentErrorOccurred";
        private const string INVALID_INSTRUMENT_STATUS_ERROR = "InvalidInstrumentStatusError";

        // Use on raising CommandExecutionNotAccepted error
        private const string NOT_ACCEPTED_MESSAGE = "Another command execution is in progress.";

        public PlateLocControllerService(IObservableCommandManager<StartCycle_Parameters, StartCycle_Responses> startCycleCommandManager,
                                         IObservableCommandManager<StopCycle_Parameters, StopCycle_Responses> stopCycleCommandManager,
                                         IObservableCommandManager<Reset_Parameters, Reset_Responses> resetCommandManager,
                                         ISiLA2Server silaServer,
                                         IConfiguration configuration,
                                         ILogger<PlateLocControllerService> logger)
        {
            // Load feature definition for validation
            _silaFeature = silaServer.ReadFeature(Path.Combine("Features", "PlateLocController-v1_0.sila.xml"));

            _startCycleCommandManager = startCycleCommandManager;
            _stopCycleCommandManager = stopCycleCommandManager;
            _resetCommandManager = resetCommandManager;
            _logger = logger;

            // Open the channel for PlateLoc Command Executor
            var channel = GrpcChannel.ForAddress($"http://localhost:{configuration["CommandExecutorConfig:Port"]}/");
            _executorClient = new PlateLocCommandExecutorService.PlateLocCommandExecutorServiceClient(channel);
        }

        #region StartCycle
        public override async Task<CommandConfirmation> StartCycle(StartCycle_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _startCycleCommandManager.AddCommand(request, StartCycleWorker);
            return command.Confirmation;
        }

        public override async Task StartCycle_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _startCycleCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<StartCycle_Responses> StartCycle_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _startCycleCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the StartCycle method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private StartCycle_Responses StartCycleWorker(IProgress<ExecutionInfo> progress, StartCycle_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.StartCycle(new StartCycleRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new StartCycle_Responses();
        }
        #endregion

        #region StopCycle
        public override async Task<CommandConfirmation> StopCycle(StopCycle_Parameters request, ServerCallContext context)
        {
            // To stop the StartCycle command, do not reject the request for the StopCycle command.
            var command = await _stopCycleCommandManager.AddCommand(request, StopCycleWorker);
            return command.Confirmation;
        }

        public override async Task StopCycle_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _stopCycleCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<StopCycle_Responses> StopCycle_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _stopCycleCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the StopCycle method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private StopCycle_Responses StopCycleWorker(IProgress<ExecutionInfo> progress, StopCycle_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.StopCycle(new StopCycleRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new StopCycle_Responses();
        }
        #endregion

        #region Reset
        public override async Task<CommandConfirmation> Reset(Reset_Parameters request, ServerCallContext context)
        {
            if (IsObservableCommandRunning())
            {
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateFrameworkError(FrameworkError.Types.ErrorType.CommandExecutionNotAccepted,
                                                                                NOT_ACCEPTED_MESSAGE));
            }

            var command = await _resetCommandManager.AddCommand(request, ResetWorker);
            return command.Confirmation;
        }

        public override async Task Reset_Info(CommandExecutionUUID request, IServerStreamWriter<ExecutionInfo> responseStream, ServerCallContext context)
        {
            try
            {
                await _resetCommandManager.RegisterForInfo(request, responseStream, context.CancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Unexpected error occurred: {ex.GetType().Name} {ex.Message}\n{ex.StackTrace}");
                throw;
            }
        }

        public override Task<Reset_Responses> Reset_Result(CommandExecutionUUID request, ServerCallContext context)
        {
            var command = _resetCommandManager.GetCommand(request);
            return Task.FromResult(command.Result);
        }

        /// <summary>
        /// Invoke the Abort method on the CommandExecutor.
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="parameters"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        private Reset_Responses ResetWorker(IProgress<ExecutionInfo> progress, Reset_Parameters parameters, CancellationToken cancellationToken)
        {
            try
            {
                var response = _executorClient.Abort(new AbortRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Reset_Responses();
        }
        #endregion

        #region Unobservable Commands
        public override async Task<SetSealingTemperature_Responses> SetSealingTemperature(SetSealingTemperature_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.SealingTemperature.Value, _silaFeature, "SetSealingTemperature", "SealingTemperature");

            try
            {
                var paramForExecutor = new SetSealingTemperatureRequest { SealingTemperature = Convert.ToInt32(request.SealingTemperature.Value) };
                var response = await _executorClient.SetSealingTemperatureAsync(paramForExecutor);
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new SetSealingTemperature_Responses();
        }

        public override async Task<SetSealingTime_Responses> SetSealingTime(SetSealingTime_Parameters request, ServerCallContext context)
        {
            Validation.ValidateParameter(request.SealingTime.Value, _silaFeature, "SetSealingTime", "SealingTime");

            try
            {
                var paramForExecutor = new SetSealingTimeRequest { SealingTime = request.SealingTime.Value };
                var response = await _executorClient.SetSealingTimeAsync(paramForExecutor);
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new SetSealingTime_Responses();
        }

        public override async Task<EnumerateProfiles_Responses> EnumerateProfiles(EnumerateProfiles_Parameters request, ServerCallContext context)
        {
            var profiles = new EnumerateProfilesResponse();
            try
            {
                profiles = await _executorClient.EnumerateProfilesAsync(new EnumerateProfilesRequest());
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            var response = new EnumerateProfiles_Responses();
            foreach (var name in profiles.ProfileNames)
            {
                response.ProfileNames.Add(new Sila2.Org.Silastandard.String { Value = name });
            }
            return response;
        }
        #endregion

        #region Properties
        public override async Task<Get_SealingTemperature_Responses> Get_SealingTemperature(Get_SealingTemperature_Parameters request, ServerCallContext context)
        {
            var temperature = 0;
            try
            {
                var response = await _executorClient.GetSealingTemperatureAsync(new GetSealingTemperatureRequest());
                temperature = response.SealingTemperature;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_SealingTemperature_Responses { SealingTemperature = new Integer { Value = temperature } };
        }

        public override async Task<Get_SealingTime_Responses> Get_SealingTime(Get_SealingTime_Parameters request, ServerCallContext context)
        {
            var time = 0.0;
            try
            {
                var response = await _executorClient.GetSealingTimeAsync(new GetSealingTimeRequest());
                time = response.SealingTime;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_SealingTime_Responses { SealingTime = new Real { Value = time } };
        }

        public override async Task<Get_ActualTemperature_Responses> Get_ActualTemperature(Get_ActualTemperature_Parameters request, ServerCallContext context)
        {
            var temperature = 0;
            try
            {
                var response = await _executorClient.GetActualTemperatureAsync(new GetActualTemperatureRequest());
                temperature = response.ActualTemperature;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_ActualTemperature_Responses { ActualTemperature = new Integer { Value = temperature } };
        }

        public override async Task<Get_CycleCount_Responses> Get_CycleCount(Get_CycleCount_Parameters request, ServerCallContext context)
        {
            var cycleCount = 0;
            try
            {
                var response = await _executorClient.GetCycleCountAsync(new GetCycleCountRequest());
                cycleCount = response.CycleCount;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_CycleCount_Responses { CycleCount = new Integer { Value = cycleCount } };
        }

        public override async Task<Get_FirmwareVersion_Responses> Get_FirmwareVersion(Get_FirmwareVersion_Parameters request, ServerCallContext context)
        {
            var firmwareVersion = "";
            try
            {
                var response = await _executorClient.GetFirmwareVersionAsync(new GetFirmwareVersionRequest());
                firmwareVersion = response.FirmwareVersion;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_FirmwareVersion_Responses { FirmwareVersion = new Sila2.Org.Silastandard.String { Value = firmwareVersion } };
        }

        public override async Task<Get_Version_Responses> Get_Version(Get_Version_Parameters request, ServerCallContext context)
        {
            var version = "";
            try
            {
                var response = await _executorClient.GetVersionAsync(new GetVersionRequest());
                version = response.Version;
            }
            catch (RpcException ex)
            {
                var errorType = GetErrorType(ex);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateDefinedExecutionError(
                    _silaFeature.GetFullyQualifiedDefinedExecutionErrorIdentifier(errorType),
                    ex.Message));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }

            return new Get_Version_Responses { Version = new Sila2.Org.Silastandard.String { Value = version } };
        }

        public override async Task Subscribe_Status(Subscribe_Status_Parameters request, IServerStreamWriter<Subscribe_Status_Responses> responseStream, ServerCallContext context)
        {
            var cts = new CancellationTokenSource();
            var callToExecutor = _executorClient.SubscribeStatus(new SubscribeStatusRequest(), cancellationToken: cts.Token);
            try
            {
                await foreach (var update in callToExecutor.ResponseStream.ReadAllAsync(cts.Token))
                {
                    // Notify that the status changed to SiLA Client 
                    await responseStream.WriteAsync(new Subscribe_Status_Responses { Status = new Integer { Value = update.Status } });
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Subscribe Status is cancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                ErrorHandling.RaiseSiLAError(ErrorHandling.CreateUndefinedExecutionError(ex.Message));
            }
        }
        #endregion

        private string GetErrorType(RpcException exception)
        {
            switch (exception.StatusCode)
            {
                case StatusCode.Unavailable:
                    return COMMAND_IS_RUNNING;

                case StatusCode.FailedPrecondition:
                    return exception.Message.Contains("InvalidStatus") ? INVALID_INSTRUMENT_STATUS_ERROR : NOT_INITIALIZED;

                case StatusCode.Aborted:
                    return COMMAND_EXECUTION_ABORTED;

                case StatusCode.Internal:
                default:
                    return INSTRUMENT_ERROR_OCCURRED;
            }
        }

        private bool IsObservableCommandRunning()
        {
            return _startCycleCommandManager.IsCommandMapBusy ||
                   _stopCycleCommandManager.IsCommandMapBusy ||
                   _resetCommandManager.IsCommandMapBusy;
        }
    }
}
