
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon;

public class TestLogger : UdonSharpBehaviour {
    public OttLogger logger;
    void Start() {
        for (int i=0; i<15; i++) {
            logger.Log(this, $"Logging message number {i}");
        }

        for (int i=0; i<15; i++) {
            logger.LogWarn(this, $"Logging message number {i}");
        }

        for (int i=0; i<15; i++) {
            logger.LogError(this, $"Logging message number {i}");
        }
    }
}
