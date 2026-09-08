import * as signalR from '@microsoft/signalr';

let connection: signalR.HubConnection | null = null;

export function getSignalRConnection(): signalR.HubConnection {
  if (!connection) {
    connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/dashboard', {
        skipNegotiation: false,
        transport: signalR.HttpTransportType.WebSockets | signalR.HttpTransportType.LongPolling,
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();
  }
  return connection;
}

export async function startSignalRConnection(): Promise<void> {
  const conn = getSignalRConnection();
  if (conn.state === signalR.HubConnectionState.Disconnected) {
    try {
      await conn.start();
      console.log('[SignalR] Connected to DashboardHub');
    } catch (err) {
      console.warn('[SignalR] Connection error, will retry in background:', err);
    }
  }
}
