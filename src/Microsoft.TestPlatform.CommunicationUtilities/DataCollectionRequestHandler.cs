// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection
{
    using System;
    using System.Collections.ObjectModel;

    using Microsoft.VisualStudio.TestPlatform.Common.DataCollection;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.DataCollection.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.Interfaces;
    using Microsoft.VisualStudio.TestPlatform.CommunicationUtilities.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel;
    using Microsoft.VisualStudio.TestPlatform.ObjectModel.Logging;

    /// <summary>
    /// The data collection request handler interface.
    /// </summary>
    internal class DataCollectionRequestHandler : IDataCollectionRequestHandler, IDisposable
    {
        private readonly ICommunicationManager communicationManager;
        private IDataSerializer dataSerializer;

        private static DataCollectionRequestHandler RequestHandler;
        private static readonly object obj = new object();

        internal DataCollectionRequestHandler()
            : this(new SocketCommunicationManager(), JsonDataSerializer.Instance)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DataCollectionRequestHandler"/> class. 
        /// </summary>
        /// <param name="communicationManager">
        /// </param>
        /// <param name="dataSerializer">
        /// </param>
        internal DataCollectionRequestHandler(ICommunicationManager communicationManager, IDataSerializer dataSerializer)
        {
            this.communicationManager = communicationManager;
            this.dataSerializer = dataSerializer;
        }

        /// <summary>
        /// Gets singleton instance of DataCollectionRequestHandler.
        /// </summary>
        public static DataCollectionRequestHandler Instance
        {
            get
            {
                lock (obj)
                {
                    if (RequestHandler == null)
                    {
                        RequestHandler = new DataCollectionRequestHandler();
                    }
                    return RequestHandler;
                }
            }
        }

        /// <summary>
        /// The dispose.
        /// </summary>
        public void Dispose()
        {
            this.communicationManager?.StopClient();
        }

        /// <summary>
        /// Closes the connection
        /// </summary>
        public void Close()
        {
            this.Dispose();
            EqtTrace.Info("Closing the connection !");
        }

        /// <inheritdoc />
        public void InitializeCommunication(int port)
        {
            this.communicationManager.SetupClientAsync(port);
        }

        /// <inheritdoc />
        public bool WaitForRequestSenderConnection(int connectionTimeout)
        {
            return this.communicationManager.WaitForServerConnection(connectionTimeout);
        }

        /// <summary>
        /// Process requests.
        /// </summary>
        public void ProcessRequests()
        {
            bool isSessionEnd = false;

            do
            {
                var message = this.communicationManager.ReceiveMessage();
                switch (message.MessageType)
                {
                    case MessageType.BeforeTestRunStart:
                        // TODO: Send actual BeforeTestRunStartResult
                        // string settingXml = message.Payload.ToObject<string>();
                        this.communicationManager.SendMessage(MessageType.BeforeTestRunStartResult, new BeforeTestRunStartResult(null, true, 0));
                        break;

                    case MessageType.AfterTestRunEnd:
                        // TODO: Send actual collection of AttachmentSet
                        this.communicationManager.SendMessage(MessageType.AfterTestRunEndResult, new Collection<AttachmentSet>());
                        EqtTrace.Info("Session End message received from server. Closing the connection.");

                        // TODO: Check if we need a separate message for closing the session.
                        isSessionEnd = true;
                        this.Close();
                        break;
                    default:
                        EqtTrace.Info("Invalid Message types");
                        break;
                }
            }
            while (!isSessionEnd);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public void SendDataCollectionMessage(DataCollectionMessageEventArgs args)
        {
            this.communicationManager.SendMessage(MessageType.DataCollectionMessage, args);
        }
    }
}