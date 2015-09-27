using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FaceLogger
{
    public partial class CameraSelectorForm : Form
    {
        public int SelectedIndex { get; private set; }
        public CameraSelectorForm(string[] name)
        {
            InitializeComponent();

            this.comboBox1.Items.AddRange(name);
            this.SelectedIndex = 0;
            this.comboBox1.SelectedItem = this.comboBox1.Items[0];
        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.SelectedIndex = this.comboBox1.SelectedIndex;
            this.Close();
        }

    }
}
