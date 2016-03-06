using System;
using System.Reflection;


namespace CB.Dynamic.CompilerServices
{
    public class EventHandlerInfo: IEquatable<EventHandlerInfo>
    {
        #region  Constructors & Destructors
        public EventHandlerInfo(EventInfo eventInfo, string handlerName,
            string handlerDefinition)
        {
            if (eventInfo == null) throw new ArgumentNullException(nameof(eventInfo));
            EventInfo = eventInfo;
            HandlerName = handlerName;
            HandlerDefinition = handlerDefinition;
        }
        #endregion


        #region  Properties & Indexers
        public EventInfo EventInfo { get; }
        public string HandlerDefinition { get; set; }
        public string HandlerName { get; set; }
        #endregion


        #region Methods
        public override bool Equals(object obj)
            => obj is EventHandlerInfo && Equals((EventHandlerInfo)obj);

        public bool Equals(EventHandlerInfo other) => EventInfo == other.EventInfo;

        public override int GetHashCode() => EventInfo?.GetHashCode() ?? 0;
        #endregion
    }
}