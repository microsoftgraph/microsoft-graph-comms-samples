# Description

This resource is used to set advanced properties on a network adapter
by setting the value of a registry keyword.
The list of available registry keywords will be dependent on the network
adapter driver.
**Please check the supported registry keywords for your adapter before
creating a configuration.**

The following is a list of common registry keywords that may apply to
your network adapter:

- AdaptiveIFS
- ITR
- LogLinkStateEvent
- MasterSlave
- NetworkAddress
- MaxRxRing1Length
- NumRxBuffersSmall
- RxIntModeration
- RxIntModerationProfile
- TxIntModerationProfile
- VlanID
- WaitAutoNegComplete
- *DcbxMode
- *EncapsulatedPacketTaskOffload
- *FlowControl
- *InterruptModeration
- *IPChecksumOffloadIPv4
- *JumboPacket
- *LsoV2IPv4
- *LsoV2IPv6
- *MaxRssProcessors
- *NetworkDirect
- *NumaNodeId
- *NumRssQueues
- *PacketDirect
- *PriorityVLANTag
- *QOS
- *ReceiveBuffers
- *RecvCompletionMethod
- *RoceMaxFrameSize
- *RscIPv4
- *RSS
- *RssBaseProcNumber
- *RssMaxProcNumber
- *RssOnHostVPorts
- *RSSProfile
- *SpeedDuplex
- *Sriov
- *TCPChecksumOffloadIPv4
- *TCPChecksumOffloadIPv6
- *TCPUDPChecksumOffloadIPv4
- *TCPUDPChecksumOffloadIPv6
- *TransmitBuffers
- *UDPChecksumOffloadIPv4
- *UDPChecksumOffloadIPv6
- *VMQ
- *VMQVlanFiltering
