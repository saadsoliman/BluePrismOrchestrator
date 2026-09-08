import { useEffect, useState } from 'react';
import { getSignalRConnection, startSignalRConnection } from '../api/signalr';
import * as signalR from '@microsoft/signalr';

export function useSignalR() {
  const [isConnected, setIsConnected] = useState(false);

  useEffect(() => {
    const conn = getSignalRConnection();

    const handleReconnecting = () => setIsConnected(false);
    const handleReconnected = () => setIsConnected(true);
    const handleClose = (error?: Error) => {
      setIsConnected(false);
      // If the close is due to an error SignalR will attempt an automatic reconnect
      // per the configured back-off intervals. No manual restart needed.
    };

    conn.onreconnecting(handleReconnecting);
    conn.onreconnected(handleReconnected);
    conn.onclose(handleClose);

    void startSignalRConnection().then(() => {
      setIsConnected(conn.state === signalR.HubConnectionState.Connected);
    });

    return () => {
      // SignalR v8 does not expose an off() for lifecycle methods registered via
      // conn.onreconnecting/onclose. They are tied to the connection instance,
      // which is a module-singleton. Deregistration here is intentionally a no-op
      // to avoid the dead-code cleanup that would mislead future maintainers.
      // Event handlers registered via on() (used by App.tsx) are cleaned up there.
    };
  }, []);

  const on = (eventName: string, callback: (...args: any[]) => void) => {
    const conn = getSignalRConnection();
    conn.on(eventName, callback);
    return () => {
      conn.off(eventName, callback);
    };
  };

  return { isConnected, on };
}
