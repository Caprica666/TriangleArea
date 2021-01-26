using UnityEngine;
using System.IO;
using System;

public class FileLogger : ILogHandler
{
    public int DebugLevel = 0;

    private FileStream m_FileStream;
    private StreamWriter m_StreamWriter;
    private ILogHandler m_DefaultLogHandler = Debug.unityLogger.logHandler;

    public FileLogger(ILogHandler defaultLogHandler)
    {
        string filePath = Application.persistentDataPath + "/debuglog.txt";

        m_FileStream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite);
        m_StreamWriter = new StreamWriter(m_FileStream);
        m_DefaultLogHandler = defaultLogHandler;
        // Replace the default debug log handler
        Debug.unityLogger.logHandler = this;
    }

    public void LogFormat(LogType logType, UnityEngine.Object context, string format, params object[] args)
    {
        if (DebugLevel > 0)
        {
            m_StreamWriter.WriteLine(String.Format(format, args));
            m_StreamWriter.Flush();
            m_DefaultLogHandler.LogFormat(logType, context, format, args);
        }
    }

    public void LogException(Exception exception, UnityEngine.Object context)
    {
        m_DefaultLogHandler.LogException(exception, context);
    }
}
