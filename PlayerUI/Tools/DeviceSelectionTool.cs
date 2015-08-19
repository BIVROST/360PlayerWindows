using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Direct3D11;
using SharpDX.DXGI;

namespace PlayerUI.Tools
{
    public class DeviceSelectionTool
    {

        public static void EnumerateGraphicCards()
        {
            Factory1 dxgiFactory = new Factory1();
            string adapters = "";
            dxgiFactory.Adapters.ToList().ForEach(adapter =>
            {
                adapters += adapter.Description.Description + "\n";
            });
            //System.Windows.MessageBox.Show(adapters);
            
        }

    }
}
