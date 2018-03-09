using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Bivrost.Bivrost360Player.Tools
{
    public class TimeoutBool
    {
        private DateTime _setTime;
        private bool _value = false;
        public bool Value
        {
            get {
                if(_value == true)
                {
                    if ((DateTime.Now - _setTime).TotalMilliseconds > Timeout)
                        Value = false;
                }
                Console.WriteLine("TIMEOUT BOOL GET: " + _value);
                return _value;
            }
            set {
                this._value = value;
                _setTime = DateTime.Now;
                Console.WriteLine("TIMEOUT BOOL SET: " + _value + " TIMEOUT: " + Timeout);
            }
        }

        public double Timeout { get; set; }

        public TimeoutBool() { }
        public TimeoutBool(bool initialValue)
        {
            Timeout = 6000;
            Value = initialValue;
        }

        public TimeoutBool(bool initialValue, double timeout)
        {
            Value = initialValue;
            Timeout = timeout;
        }

        public static implicit operator bool (TimeoutBool tb)
        {
            return tb.Value;
        }
        
        public static implicit operator TimeoutBool(bool b)
        {
            return new TimeoutBool(b);
        }
    }
}
