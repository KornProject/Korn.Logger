using Korn.Logger.Core;
using Korn.Logger.Core.Client;

namespace Korn
{
    public class KornCrasher
    {
        public KornCrasher(KornLogger logger) => this.logger = logger;

        KornLogger logger;
        LoggerHandle loggerHandle => logger.Implementation.LoggerHandle;

        public void StartWatchProcess(int processId) => KornLogger.LoggerClient.Client.Send(new WatchProcessPacket(loggerHandle, processId));
    }
}