using AxPlateLocLib;
using Grpc.Core;
using Microsoft.Extensions.Configuration;
using PlateLocCommandExecutor.Models;
using Serilog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using static PlateLocCommandExecutor.PlateLocCommandExecutorService;

namespace PlateLocCommandExecutor.Services
{
    public class PlateLocCommandExecutorServiceImpl : PlateLocCommandExecutorServiceBase
    {
        private static readonly PlateLocCommandExecutorServiceImpl _instance = new PlateLocCommandExecutorServiceImpl();

        // For safety reasons, this software enforces mutual exclusion to prevent concurrent commands.
        private static readonly string s_sealMutexName = "Global\\PlateLocService";
        private static readonly Mutex s_sealMutex = new Mutex(false, s_sealMutexName);

        private static AxPlateLoc s_axPlateLoc = null;
        private static string s_profileName;
        private static bool s_isInitialized = false;

        // For subscribe status
        private static PlateLocStatus s_status = PlateLocStatus.GetInstance();
        private readonly Channel<SubscribeStatusResponse> s_queue = System.Threading.Channels.Channel.CreateUnbounded<SubscribeStatusResponse>(
            new UnboundedChannelOptions { SingleReader = true, SingleWriter = false });

        private const string NO_TASK_TO_ABORT = "No task to abort!";

        private PlateLocCommandExecutorServiceImpl() { }

        ~PlateLocCommandExecutorServiceImpl()
        {
            s_queue.Writer.TryComplete();
            s_sealMutex.Close();
        }

        #region gRPC Request
        public override Task<StartCycleResponse> StartCycle(StartCycleRequest request, ServerCallContext context)
        {
            Log.Information("StartCycle start.");

            bool isMutexGained = s_sealMutex.WaitOne(0);
            if (!isMutexGained)
            {
                var errorMessage = "StartCycle is running.";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Unavailable, errorMessage));
            }
            try
            {
                CheckInitialized();
                CheckCurrentStatusValid(PlateLocStatus.StatusCode.Idle);

                s_status.UpdateStatus(PlateLocStatus.StatusCode.Running);
                short sealingTemperature = CallGetSealingTemperature();
                double sealingTime = CallGetSealingTime();
                Log.Debug($"Try to Seal, Temperature: {sealingTemperature} Time: {sealingTime}");

                int startCycleResult = s_axPlateLoc.StartCycle();
                if (startCycleResult != 0)
                {
                    s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);
                    string startCycleError = s_axPlateLoc.GetLastError();
                    Log.Debug($"StartCycle failed with return {startCycleResult}. Error has occured: {startCycleError}");
                }

                s_status.UpdateStatus(PlateLocStatus.StatusCode.Idle);
                Log.Information("Sealing cycle complete.");
            }
            finally
            {
                s_sealMutex.ReleaseMutex();
            }

            Log.Information("StartCycle complete.");
            return Task.FromResult(new StartCycleResponse());
        }

        public override Task<StopCycleResponse> StopCycle(StopCycleRequest request, ServerCallContext context)
        {
            Log.Information("StopCycle start.");

            CheckInitialized();
            CheckCurrentStatusValid(PlateLocStatus.StatusCode.Running);

            // FIXME: Because StopCycle() may not execute on Blocking mode, the error might not be captured correctoly.
            int result = s_axPlateLoc.StopCycle();
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);
                Log.Debug($"StopCycle failed with return {result}");

                string lastErrorMessage = s_axPlateLoc.GetLastError();
                var errorMessage = $"StopCycle failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }
            else
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Idle);
                Log.Information("The running cycle has been stopped.");
            }

            Log.Information("StopCycle complete.");
            return Task.FromResult(new StopCycleResponse());
        }

        public override Task<AbortResponse> Abort(AbortRequest request, ServerCallContext context)
        {
            Log.Information("Abort start.");

            CheckInitialized();
            var expectedStatus = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Error, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatus);

            int abortResult = s_axPlateLoc.Abort();
            if (abortResult != 0)
            {
                var abortErrorMessage = s_axPlateLoc.GetLastError();
                if (abortErrorMessage == NO_TASK_TO_ABORT)
                {
                    // If no errors can be resolved by Abort(), "No task to abort!" will be returned.
                    Log.Debug("No task to abort.");
                }
                else
                {
                    s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);

                    Log.Debug($"Abort failed with return {abortResult}");
                    var errorMessage = $"Aborting task failed because of the following error: {abortErrorMessage}";
                    Log.Error(errorMessage);
                    throw new RpcException(new Status(StatusCode.Internal, errorMessage));
                }
            }

            s_status.UpdateStatus(PlateLocStatus.StatusCode.Idle);

            Log.Information("Abort complete.");
            return Task.FromResult(new AbortResponse());
        }

        public override Task<SetSealingTemperatureResponse> SetSealingTemperature(SetSealingTemperatureRequest request, ServerCallContext context)
        {
            var sealingTemperature = (short)request.SealingTemperature;
            Log.Information("SetSealingTemperature Start.");
            Log.Information($"SealingTemperature: {sealingTemperature}");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            int result = s_axPlateLoc.SetSealingTemperature(ref sealingTemperature);
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);

                Log.Debug($"SetSealingTemperature failed with return {result}");
                string lastErrorMessage = s_axPlateLoc.GetLastError();
                var errorMessage = $"SetSealingTemperature failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information("SetSealingTemperature complete.");
            return Task.FromResult(new SetSealingTemperatureResponse());
        }

        public override Task<SetSealingTimeResponse> SetSealingTime(SetSealingTimeRequest request, ServerCallContext context)
        {
            var sealingTime = request.SealingTime;
            Log.Information("SetSealingTime Start.");
            Log.Information($"SealingTime: {sealingTime}");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            int result = s_axPlateLoc.SetSealingTime(ref sealingTime);
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);

                Log.Debug($"SetSealingTime failed with return {result}");
                string lastErrorMessage = s_axPlateLoc.GetLastError();
                var errorMessage = $"SetSealingTime failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information("SetSealingTime complete.");
            return Task.FromResult(new SetSealingTimeResponse());
        }

        public override Task<EnumerateProfilesResponse> EnumerateProfiles(EnumerateProfilesRequest request, ServerCallContext context)
        {
            Log.Information("EnumerateProfiles start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var profiles = (s_axPlateLoc.EnumerateProfiles() as string[]);
            var response = new EnumerateProfilesResponse();
            response.ProfileNames.AddRange(profiles);

            Log.Information($"EnumerateProfiles complete.");
            Log.Information($"ProfileNames: {string.Join(", ", profiles)}");
            return Task.FromResult(response);
        }

        public override Task<GetSealingTemperatureResponse> GetSealingTemperature(GetSealingTemperatureRequest request, ServerCallContext context)
        {
            Log.Information("GetSealingTemperature start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var temperature = CallGetSealingTemperature();
            if (temperature < 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);
                var errorMessage = "Failed to get sealing temperature.";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information($"GetSealingTemperature complete.");
            Log.Information($"SealingTemperature: {temperature}");
            return Task.FromResult(new GetSealingTemperatureResponse { SealingTemperature = Convert.ToInt32(temperature) });
        }

        public override Task<GetSealingTimeResponse> GetSealingTime(GetSealingTimeRequest request, ServerCallContext context)
        {
            Log.Information("GetSealingTime start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var time = CallGetSealingTime();
            if (time < 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);
                var errorMessage = "Failed to get sealing time.";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information("GetSealingTime complete.");
            Log.Information($"SealingTime; {time}");
            return Task.FromResult(new GetSealingTimeResponse { SealingTime = time });
        }

        public override Task<GetActualTemperatureResponse> GetActualTemperature(GetActualTemperatureRequest request, ServerCallContext context)
        {
            Log.Information("GetActualTemperature start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            short actualTemperature = 0;
            int result = s_axPlateLoc.GetActualTemperature(ref actualTemperature);
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);

                Log.Debug($"GetActualTemperature failed with return {result}");
                string lastErrorMessage = s_axPlateLoc.GetLastError();
                var errorMessage = $"GetActualTemperature failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information("GetActualTemperature complete.");
            Log.Information($"ActualTemperature: {actualTemperature}");
            return Task.FromResult(new GetActualTemperatureResponse { ActualTemperature = actualTemperature });
        }

        public override Task<GetCycleCountResponse> GetCycleCount(GetCycleCountRequest request, ServerCallContext context)
        {
            Log.Information("GetCycleCount start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            int cycleCount = 0;
            int result = s_axPlateLoc.GetCycleCount(ref cycleCount);
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);

                Log.Debug($"GetCycleCount failed with return {result}");
                string lastErrorMessage = s_axPlateLoc.GetLastError();
                var errorMessage = $"GetCycleCount failed because of the following error: {lastErrorMessage}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.Internal, errorMessage));
            }

            Log.Information($"GetCycleCount complete.");
            Log.Information($"CycleCount: {cycleCount}");
            return Task.FromResult(new GetCycleCountResponse { CycleCount = cycleCount });
        }

        public override Task<GetFirmwareVersionResponse> GetFirmwareVersion(GetFirmwareVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetFirmwareVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var result = s_axPlateLoc.GetFirmwareVersion();
            Log.Information($"GetFirmwareVersion complete.");
            Log.Information($"FirmwareVersion: {result}");
            return Task.FromResult(new GetFirmwareVersionResponse { FirmwareVersion = result });
        }

        public override Task<GetVersionResponse> GetVersion(GetVersionRequest request, ServerCallContext context)
        {
            Log.Information("GetVersion start.");

            CheckInitialized();
            var expectedStatuses = new List<PlateLocStatus.StatusCode> { PlateLocStatus.StatusCode.Idle, PlateLocStatus.StatusCode.Running };
            CheckCurrentStatusValid(expectedStatuses);

            var result = s_axPlateLoc.GetVersion();
            Log.Information($"GetVersion complete.");
            Log.Information($"Version: {result}");
            return Task.FromResult(new GetVersionResponse { Version = result });
        }

        public override async Task SubscribeStatus(SubscribeStatusRequest request, IServerStreamWriter<SubscribeStatusResponse> responseStream, ServerCallContext context)
        {
            void handler(object s, PropertyChangedEventArgs e)
            {
                if (e.PropertyName == nameof(PlateLocStatus.Status) && s is PlateLocStatus)
                {
                    s_queue.Writer.TryWrite(new SubscribeStatusResponse { Status = (int)(s as PlateLocStatus).Status });
                }
            }

            Log.Information($"Start to subscribe Status. Current Status: {s_status.Status}");

            s_queue.Writer.TryWrite(new SubscribeStatusResponse { Status = (int)s_status.Status });
            s_status.PropertyChanged += handler;
            try
            {
                while (!context.CancellationToken.IsCancellationRequested)
                {
                    if (await s_queue.Reader.WaitToReadAsync(context.CancellationToken) && s_queue.Reader.TryRead(out var update))
                    {
                        await responseStream.WriteAsync(update);
                    }
                }
            }
            finally
            {
                s_status.PropertyChanged -= handler;
                Log.Information("Stop subscribing Status.");
            }
        }
        #endregion

        #region Service Basics
        public static PlateLocCommandExecutorServiceImpl GetService()
        {
            return _instance;
        }

        /// <summary>
        /// Get the profile name from config file and try to connect the PlateLoc.
        /// </summary>
        /// <param name="form"></param>
        /// <param name="appConfiguration"></param>
        public void Initialize(PlateLocForm form, IConfigurationSection appConfiguration)
        {
            Log.Information("Initialize Start.");

            s_axPlateLoc = form.GetControl();

            s_profileName = appConfiguration["profileName"];
            Log.Debug($"profileName read: {s_profileName}");

            Connect();
        }

        private void Connect()
        {
            int result = s_axPlateLoc.Initialize(s_profileName);

            if (result == 0)
            {
                s_isInitialized = true;
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Idle);
                Log.Information($"Device initialized.");
            }
            else
            {
                Log.Debug($"Initialize failed with return {result}");
                s_isInitialized = false;
                s_status.UpdateStatus(PlateLocStatus.StatusCode.NotConnected);
                Log.Warning($"Failed to initialize: {s_axPlateLoc.GetLastError()}");
            }
        }

        internal void Close()
        {
            Log.Information("Closing device.");
            int result = s_axPlateLoc.Close();
            if (result != 0)
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.Error);
                Log.Debug($"Close failed with return {result}");
                Log.Warning($"Failed to close connection: {s_axPlateLoc.GetLastError()}");
            }
            else
            {
                s_status.UpdateStatus(PlateLocStatus.StatusCode.NotConnected);
                Log.Information("Closed device successfully.");
            }
        }
        #endregion

        #region private methods
        private void CheckInitialized()
        {
            if (!s_isInitialized)
            {
                Log.Information("Retrying to initialize device.");
                Connect();
                if (!s_isInitialized)
                {
                    var errorMessage = "PlateLoc is not initialized.";
                    Log.Error(errorMessage);
                    throw new RpcException(new Status(StatusCode.FailedPrecondition, errorMessage));
                }
            }
        }

        private void CheckCurrentStatusValid(PlateLocStatus.StatusCode validStatus)
        {
            CheckCurrentStatusValid(new List<PlateLocStatus.StatusCode> { validStatus });
        }

        private void CheckCurrentStatusValid(List<PlateLocStatus.StatusCode> validStatuses)
        {
            var validStatusesStr = string.Join(", ", validStatuses.Select(s => Enum.GetName(typeof(PlateLocStatus.StatusCode), s)));
            Log.Debug($"Check current status is valid or not. Expected: {validStatusesStr}");

            if (!s_status.IsStatusValid(validStatuses))
            {
                var errorMessage = $"InvalidStatus. Expected: {validStatusesStr} / Actual: {Enum.GetName(typeof(PlateLocStatus.StatusCode), s_status.Status)}";
                Log.Error(errorMessage);
                throw new RpcException(new Status(StatusCode.FailedPrecondition, errorMessage));
            }
        }

        private short CallGetSealingTemperature()
        {
            short sealingTemperature = -1;
            int getTemparetureResult = s_axPlateLoc.GetSealingTemperature(ref sealingTemperature);
            if (getTemparetureResult != 0)
            {
                // This method is also called at sealing, so log the event but do not throw exceptions.
                Log.Debug($"Get sealing temperature failed with return {getTemparetureResult}");
                Log.Warning($"Failed to get sealing temperature: {s_axPlateLoc.GetLastError()}");
            }
            return sealingTemperature;
        }

        private double CallGetSealingTime()
        {
            double sealingTime = -1;
            int getTimeResult = s_axPlateLoc.GetSealingTime(ref sealingTime);
            if (getTimeResult != 0)
            {
                // This method is also called at sealing, so log the event but do not throw exceptions.
                Log.Debug($"Get sealing time failed with return {getTimeResult}");
                Log.Warning($"Failed to get sealing time: {s_axPlateLoc.GetLastError()}");
            }
            return sealingTime;
        }
        #endregion
    }
}
