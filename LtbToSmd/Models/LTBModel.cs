using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SevenZip.Compression.LZMA;
using LtbToSmd.ViewModels;

namespace LtbToSmd.Models
{

    public class LtbModel
    {
        private MainWindowViewModel _MainWindowViewModel;

        public LtbModel(MainWindowViewModel vm)
        {
            _InputFiles = new List<string>();
            _MainWindowViewModel = vm;
            CultureInfo culture = (CultureInfo)CultureInfo.CurrentCulture.Clone();
        }

        public void AddInputFile(string file)
        {
            if (_InputFiles != null)
            {
                _InputFiles.Add(file);
            }
        }

        private void AppendLog(string log)
        {
            _MainWindowViewModel.AddLog(log);
        }

        public void ClearInputFiles()
        {
            _InputFiles.Clear();
        }

        
        private void ConvertToSmd(string file)
        {
            totalmesh = 0;
            is__doing = true;
            // richTextBox1.AppendText("\n");
            int pos_ext = file.LastIndexOf(".");
            int pos_Path = file.LastIndexOf("\\");
            string fname = file.Substring(pos_Path + 1, pos_ext - pos_Path - 1);
            string gPath = file.Substring(0, pos_Path + 1);

            cur_fName = fname;
            cur_Path = gPath;
            // richTextBox1.AppendText("[+] Path:" + gPath + "\n");
            // richTextBox1.AppendText("[+] file:" + fname + ".ltb\n");

            FileStream fileStream = new FileStream(file, FileMode.Open);
            _LTBFile = new BinaryReader(fileStream);
            // 从文件中读取数据
            // richTextBox1.AppendText("[+] Lấy dữ liệu trong file:" + fname + ".ltb\n");

            int Check_header = _LTBFile.ReadUInt16(); _LTBFile.ReadUInt16();
            _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32();
            // 检查文件类型
            //richTextBox1.AppendText("[+] Kiểm tra kiểu file\n");
            Boolean is_tmp = false;
            if (Check_header > 20)
            {
                _LTBFile.Close();
                //LZMA压缩文件
                //richTextBox1.AppendText("   [!] File pack lmza\n");
                //进行解压缩
                //richTextBox1.AppendText("   - Tiến hành decompress\n");
                Decompress_file(file, "___tmp.tmp");
                is_tmp = true;
                //重新读取文件
                //richTextBox1.AppendText("[+] Tiến hành đọc lại file\n");
                fileStream = new FileStream("___tmp.tmp", FileMode.Open);
                _LTBFile = new BinaryReader(fileStream);

                _LTBFile.ReadUInt16(); _LTBFile.ReadUInt16();
                _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32();

            }
            UInt32 version = _LTBFile.ReadUInt32();

            _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32();
            numBones = _LTBFile.ReadUInt32();
            _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32(); _LTBFile.ReadUInt32();
            _LTBFile.ReadUInt16(); _LTBFile.ReadUInt16();
            _LTBFile.ReadUInt32();
            _LTBFile.ReadBytes(_LTBFile.ReadUInt16());
            _LTBFile.ReadSingle();
            _LTBFile.ReadUInt32();
            numMesh = _LTBFile.ReadUInt32();
            // version文件版本
            //richTextBox1.AppendText("     - File version:" + version + "\n");
            // 初始化存储变量
            //richTextBox1.AppendText("     - Khởi tạo biến lưu trữ\n");

            _MeshData = new List<CMeshData>();
            _BoneData = new List<CBoneData>();
            //提取基础数据
            //richTextBox1.AppendText("     - Lấy dữ liệu cơ sở\n");
            Parse_mesh(_LTBFile, numMesh);
            numMesh = totalmesh;
            if (is__doing == false)
            {
                //过程失败 [不支持此文件类型]
                //richTextBox1.AppendText("     - Quá trình thất bại [Chư hỗ trợ kiểu file này] \n");
                _LTBFile.Close();
                return;
            }
            Calc_weightsets();
            parse_skeleton(_LTBFile);
            //基础计算
            //richTextBox1.AppendText("     - Tính toán cơ sở\n");
            Clac_Par_Bone();

            if (isCheckboxAnim == true)
            {
                //检查动画
                //richTextBox1.AppendText("     - Kiểm tra anim\n");
                if (_LTBFile.BaseStream.Length - _LTBFile.BaseStream.Position < 2048) isAnim = false;
                else isAnim = true;
                //  isAnim = false;
                if (isAnim == true)
                {
                    //提取动画数据
                    //richTextBox1.AppendText("           + Lấy dữ liệu về anim\n");
                    Parse_animation(_LTBFile);
                    //有N个动画
                    //richTextBox1.AppendText("               * Có " + numAnim + " anim\n");
                }
                //else //无法从此文件中提取动画
                //    richTextBox1.AppendText("           + Không lấy được anim trong file này\n");
            }
            else isAnim = false;
            //将网格写入文件
            //richTextBox1.AppendText("[+] Ghi mesh vào file:" + fname + ".smd\n");
            //  Scale_();
            calc_databone();
            //  get_new_bone_out_data(0, 0, 1.0f,1.0f,0.65f);
            //   Change_a_anim(1.0f, 1.0f, 0.65f);
            Write_SMD_MODEL(gPath + fname);
            if (isAnim == true)
                for (int i = 0; i < numAnim; i++)
                {
                    //将动画写入文件
                    //richTextBox1.AppendText("[+] Ghi Anim " + _AnimData[i].name + " vào file:" + _AnimData[i].name + ".smd\n");
                    Write_SMD_ANIM(i, gPath + _AnimData[i].name + ".smd");
                }
            if (isAutoCreateQC == true && isAnim == true)
            {
                //创建QC文件
                //richTextBox1.AppendText("[+] Tạo file QC file:" + fname + ".qc\n");
                Write_QC(gPath + fname + ".qc", fname);
            }
            //转换完成
            //richTextBox1.AppendText("Hoàn thành!! Bạn có thể kéo lên để xem lại thông báo của quá trình\n");
            _LTBFile.Close();
            if (is_tmp == true) File.Delete("___tmp.tmp");

        }

        #region CalcData
        private void Calc_weightsets()
        {
            for (int i = 0; i < numMesh; i++)
            {

                if (_MeshData[i].weightsets.Count > 1)
                {
                    int pWeightset = 0;
                    for (int j = 0; j < _MeshData[i].weightsets.Count; j++)
                    {
                        int[] intWeightSet = new int[5];

                        int num = 0;
                        for (int n = 0; n < 4; n++)
                        {
                            if (_MeshData[i].weightsets[j][2 + n] > -1)
                            {
                                intWeightSet[n + 1] = _MeshData[i].weightsets[j][2 + n];
                                num += 1;

                            }
                            else break;

                        }
                        intWeightSet[0] = num;

                        for (int k = 0; k < _MeshData[i].weightsets[j][1]; k++)
                        {
                            float[] WeightsetSize = new float[7];
                            if (_MeshData[i].weights.Count > 0)
                            {
                                WeightsetSize = _MeshData[i].weights[pWeightset];

                                int outw = GetBoneWeightD(intWeightSet, WeightsetSize);
                                _MeshData[i].weightsets_output.Add(outw);
                            }
                            else _MeshData[i].weightsets_output.Add(_MeshData[i].weightsets[j][2]);

                            pWeightset += 1;
                        }
                    }
                }
                else
                {
                    for (int j = 0; j < _MeshData[i].nvertices; j++)
                        _MeshData[i].weightsets_output.Add(_MeshData[i].weightsets[0][0]);
                }
            }
            // weightsets_output
        }
        private float[] QuaternionSlerp(float[] p, float[] q, float t)

        {
            float[] qt = new float[4];
            int i;
            double omega, cosom, sinom, sclp, sclq;

            // decide if one of the quaternions is backwards
            float a = 0;
            float b = 0;
            for (i = 0; i < 4; i++)
            {
                a += (p[i] - q[i]) * (p[i] - q[i]);
                b += (p[i] + q[i]) * (p[i] + q[i]);
            }
            if (a > b)
            {
                for (i = 0; i < 4; i++)
                {
                    q[i] = -q[i];
                }
            }

            cosom = p[0] * q[0] + p[1] * q[1] + p[2] * q[2] + p[3] * q[3];

            if ((1.0 + cosom) > 0.00000001f)
            {
                if ((1.0 - cosom) > 0.00000001f)
                {
                    omega = Math.Acos(cosom);
                    sinom = Math.Sin(omega);
                    sclp = Math.Sin((1.0f - t) * omega) / sinom;
                    sclq = Math.Sin(t * omega) / sinom;
                }
                else
                {
                    sclp = 1.0f - t;
                    sclq = t;
                }
                for (i = 0; i < 4; i++)
                {
                    qt[i] = (float)(sclp * p[i] + sclq * q[i]);
                }
            }
            else
            {
                qt[0] = -p[1];
                qt[1] = p[0];
                qt[2] = -p[3];
                qt[3] = p[2];
                sclp = Math.Sin((1.0f - t) * 0.5f * Math.PI);
                sclq = Math.Sin(t * 0.5f * Math.PI);
                for (i = 0; i < 3; i++)
                {
                    qt[i] = (float)(sclp * p[i] + sclq * qt[i]);
                }
            }
            return qt;
        }

        private void auto_calc_all_Frame(int indexanim)
        {
            if (isSubForm == false) return;
            scale_(indexanim, Scaleto);

            if (isCalcframe == false) return;
            List<int> glistkeyframe = _AnimData[indexanim].listkeyframe;
            CFramedata[] frame = _AnimData[indexanim].frame;
            List<int> newlistframe = new List<int>();
            CFramedata[] newframe = new CFramedata[numBones];

            for (int j = 0; j < numBones; j++)
            {
                newframe[j].pos = new List<float[]>();
                newframe[j].quats = new List<float[]>();

            }
            for (int j = 0; j < glistkeyframe[glistkeyframe.Count - 1]; j++)
                newlistframe.Add(j);
            for (int i = 1; i < _AnimData[indexanim].nkeyframe; i++)
            {
                for (int j = 0; j < numBones; j++)
                {
                    int nF = glistkeyframe[i] - glistkeyframe[i - 1];
                    float[] add_pos = new float[3];
                    float[] add_quats = new float[4];
                    for (int n = 0; n < 3; n++)
                        add_pos[n] = (frame[j].pos[i - 1][n] - frame[j].pos[i][n]) / ((float)nF);

                    // for (int n = 0; n < 4; n++)
                    //       add_quats[n] = (frame[j].quats[i - 1][n] - frame[j].quats[i][n]) / ((float)nF);

                    for (int k = glistkeyframe[i - 1]; k < glistkeyframe[i] - 1; k++)
                    {
                        float[] pos = new float[3];
                        float[] quats = new float[4];

                        float[] rot = new float[3];
                        for (int n = 0; n < 3; n++)

                            pos[n] = frame[j].pos[i - 1][n] + add_pos[n] * (glistkeyframe[i - 1] - k);

                        float time = (float)(k - glistkeyframe[i - 1]) / (float)nF;
                        if (time > 1.0f) time = 1.0f;
                        quats = QuaternionSlerp(frame[j].quats[i - 1], frame[j].quats[i], time);


                        // if (quats[3] > 1.0f) quats[3] -= 1.0f;
                        //  quats[3]
                        //   float lenght = (float)Math.Sqrt(quats[0] * quats[0] + quats[1] * quats[1] + quats[2] * quats[2]);


                        newframe[j].pos.Add(pos);
                        newframe[j].quats.Add(quats);
                    }
                    newframe[j].pos.Add(frame[j].pos[i]);
                    newframe[j].quats.Add(frame[j].quats[i]);

                }

            }
            _AnimData[indexanim].listkeyframe.Clear();
            _AnimData[indexanim].listkeyframe = newlistframe;
            _AnimData[indexanim].nkeyframe = Convert.ToUInt32(newlistframe.Count);
            _AnimData[indexanim].frame = newframe;
        }

        private void scale_(int indexanim, float sizemax)
        {
            float nfScale = 1.0f;

            float maxframe = (float)_AnimData[indexanim].listkeyframe[(int)_AnimData[indexanim].nkeyframe - 1];
            if (maxframe > sizemax && isAutoScaler == true)
                nfScale = maxframe / sizemax;
            for (int i = 0; i < _AnimData[indexanim].nkeyframe; i++)
            {
                float giTime = (float)_AnimData[indexanim].listkeyframe[i] / nfScale;
                if (float.IsNaN(giTime)) giTime = 0.0f;
                _AnimData[indexanim].listkeyframe[i] = (int)Math.Round(giTime);

            }
            return;

        }

        private double[] rotationMatrixToEulerAngles(double[,] matrix)
        {

            double sy = Math.Sqrt(matrix[0, 0] * matrix[0, 0] + matrix[1, 0] * matrix[1, 0]);

            bool singular = sy < 1e-6; // If

            double x, y, z;
            if (!singular)
            {
                x = Math.Atan2(matrix[2, 1], matrix[2, 2]);
                y = Math.Atan2(-matrix[2, 0], sy);
                z = Math.Atan2(matrix[1, 0], matrix[0, 0]);
            }
            else
            {

                x = Math.Atan2(matrix[1, 2], matrix[1, 1]);
                y = Math.Atan2(-matrix[2, 0], sy);
                z = 0;

                if (matrix[2, 0] > 0 && matrix[0, 1] > 0)
                {
                    x = Math.Atan2(matrix[1, 2], matrix[1, 1]);
                    y = Math.Atan2(-matrix[2, 0], sy);
                    z = Math.PI;
                }
            }

            normal_rotate(x);
            normal_rotate(y);
            normal_rotate(z);
            return new double[] { x, y, z };

        }
        private double normal_rotate(double angle)
        {
            while (angle > 2 * (Math.PI)) angle -= 2 * (Math.PI);
            while (angle < 2 * (Math.PI)) angle += 2 * (Math.PI);
            return angle;

        }

        private double[,] worldToLocalMatrix(double[,] Matrix4x4, int parentIndex, List<double[,]> hMas)
        {
            double[,] localMatrix = new double[4, 4];
            double[,] UnitMatrix = new double[4, 4];
            double[,] inverseMatrix = new double[4, 8];
            if (parentIndex < 0) return Matrix4x4;

            int index = parentIndex;
            /*
            for (int i = 0; i < 4; i++)
            {
                double[] roots = solveEquations(new double[4, 5] { { hMas[index][0, 0], hMas[index][1, 0], hMas[index][2, 0], hMas[index][3, 0], UnitMatrix[i, 0] }, { hMas[index][0, 1], hMas[index][1, 1], hMas[index][2, 1], hMas[index][3, 1], UnitMatrix[i, 1] }, { hMas[index][0, 2], hMas[index][1, 2], hMas[index][2, 2], hMas[index][3, 2], UnitMatrix[i, 2] }, { hMas[index][0, 3], hMas[index][1, 3], hMas[index][2, 3], hMas[index][3, 3], UnitMatrix[i, 3] } });
                for (int j = 0; j < 4; j++)
                {
                    inverseMatrix[i, j] = roots[j];
                }
            }
            */
            inverseMatrix = InverseMat2(hMas[index]);
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    localMatrix[i, j] = inverseMatrix[i, 0] * Matrix4x4[0, j] + inverseMatrix[i, 1] * Matrix4x4[1, j] + inverseMatrix[i, 2] * Matrix4x4[2, j] + inverseMatrix[i, 3] * Matrix4x4[3, j];
                }
            }
            return localMatrix;
        }

        private double[,] InverseMat2(double[,] m)
        {
            double[,] invOut = new double[4, 4];
            double[,] inv = new double[4, 4];
            double det;
            int i;

            inv[0, 0] = m[1, 1] * m[2, 2] * m[3, 3] - m[1, 1] * m[2, 3] * m[3, 2] - m[2, 1] * m[1, 2] * m[3, 3] + m[2, 1] * m[1, 3] * m[3, 2] + m[3, 1] * m[1, 2] * m[2, 3] - m[3, 1] * m[1, 3] * m[2, 2];
            inv[1, 0] = -m[1, 0] * m[2, 2] * m[3, 3] + m[1, 0] * m[2, 3] * m[3, 2] + m[2, 0] * m[1, 2] * m[3, 3] - m[2, 0] * m[1, 3] * m[3, 2] - m[3, 0] * m[1, 2] * m[2, 3] + m[3, 0] * m[1, 3] * m[2, 2];
            inv[2, 0] = m[1, 0] * m[2, 1] * m[3, 3] - m[1, 0] * m[2, 3] * m[3, 1] - m[2, 0] * m[1, 1] * m[3, 3] + m[2, 0] * m[1, 3] * m[3, 1] + m[3, 0] * m[1, 1] * m[2, 3] - m[3, 0] * m[1, 3] * m[2, 1];
            inv[3, 0] = -m[1, 0] * m[2, 1] * m[3, 2] + m[1, 0] * m[2, 2] * m[3, 1] + m[2, 0] * m[1, 1] * m[3, 2] - m[2, 0] * m[1, 2] * m[3, 1] - m[3, 0] * m[1, 1] * m[2, 2] + m[3, 0] * m[1, 2] * m[2, 1];
            inv[0, 1] = -m[0, 1] * m[2, 2] * m[3, 3] + m[0, 1] * m[2, 3] * m[3, 2] + m[2, 1] * m[0, 2] * m[3, 3] - m[2, 1] * m[0, 3] * m[3, 2] - m[3, 1] * m[0, 2] * m[2, 3] + m[3, 1] * m[0, 3] * m[2, 2];
            inv[1, 1] = m[0, 0] * m[2, 2] * m[3, 3] - m[0, 0] * m[2, 3] * m[3, 2] - m[2, 0] * m[0, 2] * m[3, 3] + m[2, 0] * m[0, 3] * m[3, 2] + m[3, 0] * m[0, 2] * m[2, 3] - m[3, 0] * m[0, 3] * m[2, 2];
            inv[2, 1] = -m[0, 0] * m[2, 1] * m[3, 3] + m[0, 0] * m[2, 3] * m[3, 1] + m[2, 0] * m[0, 1] * m[3, 3] - m[2, 0] * m[0, 3] * m[3, 1] - m[3, 0] * m[0, 1] * m[2, 3] + m[3, 0] * m[0, 3] * m[2, 1];
            inv[3, 1] = m[0, 0] * m[2, 1] * m[3, 2] - m[0, 0] * m[2, 2] * m[3, 1] - m[2, 0] * m[0, 1] * m[3, 2] + m[2, 0] * m[0, 2] * m[3, 1] + m[3, 0] * m[0, 1] * m[2, 2] - m[3, 0] * m[0, 2] * m[2, 1];
            inv[0, 2] = m[0, 1] * m[1, 2] * m[3, 3] - m[0, 1] * m[1, 3] * m[3, 2] - m[1, 1] * m[0, 2] * m[3, 3] + m[1, 1] * m[0, 3] * m[3, 2] + m[3, 1] * m[0, 2] * m[1, 3] - m[3, 1] * m[0, 3] * m[1, 2];
            inv[1, 2] = -m[0, 0] * m[1, 2] * m[3, 3] + m[0, 0] * m[1, 3] * m[3, 2] + m[1, 0] * m[0, 2] * m[3, 3] - m[1, 0] * m[0, 3] * m[3, 2] - m[3, 0] * m[0, 2] * m[1, 3] + m[3, 0] * m[0, 3] * m[1, 2];
            inv[2, 2] = m[0, 0] * m[1, 1] * m[3, 3] - m[0, 0] * m[1, 3] * m[3, 1] - m[1, 0] * m[0, 1] * m[3, 3] + m[1, 0] * m[0, 3] * m[3, 1] + m[3, 0] * m[0, 1] * m[1, 3] - m[3, 0] * m[0, 3] * m[1, 1];
            inv[2, 3] = -m[0, 0] * m[1, 1] * m[3, 2] + m[0, 0] * m[1, 2] * m[3, 1] + m[1, 0] * m[0, 1] * m[3, 2] - m[1, 0] * m[0, 2] * m[3, 1] - m[3, 0] * m[0, 1] * m[1, 2] + m[3, 0] * m[0, 2] * m[1, 1];
            inv[0, 3] = -m[0, 1] * m[1, 2] * m[2, 3] + m[0, 1] * m[1, 3] * m[2, 2] + m[1, 1] * m[0, 2] * m[2, 3] - m[1, 1] * m[0, 3] * m[2, 2] - m[2, 1] * m[0, 2] * m[1, 3] + m[2, 1] * m[0, 3] * m[1, 2];
            inv[1, 3] = m[0, 0] * m[1, 2] * m[2, 3] - m[0, 0] * m[1, 3] * m[2, 2] - m[1, 0] * m[0, 2] * m[2, 3] + m[1, 0] * m[0, 3] * m[2, 2] + m[2, 0] * m[0, 2] * m[1, 3] - m[2, 0] * m[0, 3] * m[1, 2];
            inv[2, 3] = -m[0, 0] * m[1, 1] * m[2, 3] + m[0, 0] * m[1, 3] * m[2, 1] + m[1, 0] * m[0, 1] * m[2, 3] - m[1, 0] * m[0, 3] * m[2, 1] - m[2, 0] * m[0, 1] * m[1, 3] + m[2, 0] * m[0, 3] * m[1, 1];
            inv[3, 3] = m[0, 0] * m[1, 1] * m[2, 2] - m[0, 0] * m[1, 2] * m[2, 1] - m[1, 0] * m[0, 1] * m[2, 2] + m[1, 0] * m[0, 2] * m[2, 1] + m[2, 0] * m[0, 1] * m[1, 2] - m[2, 0] * m[0, 2] * m[1, 1];

            det = m[0, 0] * inv[0, 0] + m[0, 1] * inv[1, 0] + m[0, 2] * inv[2, 0] + m[0, 3] * inv[3, 0];

            if (det == 0)
                return invOut;

            det = 1.0f / det;

            for (i = 0; i < 4; i++)
                for (int j = 0; j < 4; j++)
                    invOut[i, j] = inv[i, j] * det;

            return invOut;
        }

        private double[] GetRotation(double[,] m)
        {

            double tr = m[0, 0] + m[1, 1] + m[2, 2];
            double x, y, z, w;
            if (tr > 0f)
            {
                double s = Math.Sqrt(1f + tr) * 2f;
                w = 0.25f * s;
                x = (m[2, 1] - m[1, 2]) / s;
                y = (m[0, 2] - m[2, 0]) / s;
                z = (m[1, 0] - m[0, 1]) / s;
            }
            else if ((m[0, 0] > m[1, 1]) && (m[0, 0] > m[2, 2]))
            {
                double s = Math.Sqrt(1f + m[0, 0] - m[1, 1] - m[2, 2]) * 2f;
                w = (m[2, 1] - m[1, 2]) / s;
                x = 0.25f * s;
                y = (m[0, 1] + m[1, 0]) / s;
                z = (m[0, 2] + m[2, 0]) / s;
            }
            else if (m[1, 1] > m[2, 2])
            {
                double s = Math.Sqrt(1f + m[1, 1] - m[0, 0] - m[2, 2]) * 2f;
                w = (m[0, 2] - m[2, 0]) / s;
                x = (m[0, 1] + m[1, 0]) / s;
                y = 0.25f * s;
                z = (m[1, 2] + m[2, 1]) / s;
            }
            else
            {
                double s = Math.Sqrt(1f + m[2, 2] - m[0, 0] - m[1, 1]) * 2f;
                w = (m[1, 0] - m[0, 1]) / s;
                x = (m[0, 2] + m[2, 0]) / s;
                y = (m[1, 2] + m[2, 1]) / s;
                z = 0.25f * s;
            }

            double[] quat = new double[] { x, y, z, w };
            return quat;
        }

        private double[] GetPosition(double[,] matrix)
        {
            var x = matrix[0, 3];
            var y = matrix[1, 3];
            var z = matrix[2, 3];
            return new double[] { x, y, z };
        }

        private double[] quaternionToRotation(double[] quaternion)
        {

            double norm = Math.Sqrt(quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2] + quaternion[3] * quaternion[3]);

            if (norm > 1.0)
            {
                quaternion[0] /= norm;
                quaternion[1] /= norm;
                quaternion[2] /= norm;
                quaternion[3] /= norm;

            }


            double[] rotation = new double[3];
            rotation[0] = Math.Atan2(2 * (quaternion[3] * quaternion[0] + quaternion[1] * quaternion[2]), 1 - 2 * (quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1]));

            rotation[1] = Math.Asin(2 * (quaternion[3] * quaternion[1] - quaternion[2] * quaternion[0]));
            rotation[2] = Math.Atan2(2 * (quaternion[3] * quaternion[2] + quaternion[0] * quaternion[1]), 1 - 2 * (quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2]));



            return rotation;

        }

        private void calc_databone()
        {
            Matrix4x4s.Clear();
            for (int i = 0; i < numBones; i++)
            {
                double[,] Matrix4x4 = new double[4, 4];
                for (int k = 0; k < 4; k++)
                {
                    Matrix4x4[k, 0] = _BoneData[i].matdata[k, 0];
                    Matrix4x4[k, 1] = _BoneData[i].matdata[k, 1];
                    Matrix4x4[k, 2] = _BoneData[i].matdata[k, 2];
                    Matrix4x4[k, 3] = _BoneData[i].matdata[k, 3];
                }
                Matrix4x4s.Add(Matrix4x4);
                double[,] localMatrix = worldToLocalMatrix(Matrix4x4, _BoneData[i].par, Matrix4x4s);
                double[] quaternion = new double[3];
                double[] rotation = new double[3];
                quaternion = GetRotation(localMatrix);

                rotation = quaternionToRotation(quaternion);
                //rotation = QuaternionToYawPitchRoll(quaternion);
                double[] position = new double[3];
                position = GetPosition(localMatrix);
                _BoneData[i].bone_data_out = position[0].ToString("F6", culture) + " " + position[1].ToString("F6", culture) + " " + position[2].ToString("F6", culture) + " " + rotation[0].ToString("F6", culture) + " " + rotation[1].ToString("F6", culture) + " " + rotation[2].ToString("F6", culture);
            }
        }
        private void Clac_Par_Bone()
        {
            uint[] nsubone = new uint[numBones];
            nsubone[0] = _BoneData[0].nSubbone;
            _BoneData[0].par = -1;
            for (int i = 1; i < numBones; i++)
            {
                nsubone[i] = _BoneData[i].nSubbone;
                for (int j = i - 1; j >= 0; j--)
                    if (nsubone[j] > 0)
                    {
                        nsubone[j] -= 1;
                        _BoneData[i].par = j;
                        break;
                    }
            }
        }
        private double[,] QuaternionMatrix(double[] quaternion)
        {
            double[,] matrix = new double[4, 4];
            matrix[0, 0] = 1.0f - 2.0f * quaternion[1] * quaternion[1] - 2.0f * quaternion[2] * quaternion[2];
            matrix[1, 0] = 2.0f * quaternion[0] * quaternion[1] + 2.0f * quaternion[3] * quaternion[2];
            matrix[2, 0] = 2.0f * quaternion[0] * quaternion[2] - 2.0f * quaternion[3] * quaternion[1];

            matrix[0, 1] = 2.0f * quaternion[0] * quaternion[1] - 2.0f * quaternion[3] * quaternion[2];
            matrix[1, 1] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[2] * quaternion[2];
            matrix[2, 1] = 2.0f * quaternion[1] * quaternion[2] + 2.0f * quaternion[3] * quaternion[0];

            matrix[0, 2] = 2.0f * quaternion[0] * quaternion[2] + 2.0f * quaternion[3] * quaternion[1];
            matrix[1, 2] = 2.0f * quaternion[1] * quaternion[2] - 2.0f * quaternion[3] * quaternion[0];
            matrix[2, 2] = 1.0f - 2.0f * quaternion[0] * quaternion[0] - 2.0f * quaternion[1] * quaternion[1];

            matrix[3, 0] = 0.0f;
            matrix[3, 1] = 0.0f;
            matrix[3, 2] = 0.0f;
            matrix[3, 3] = 1.0f;
            return matrix;
        }

        #endregion

        #region WriteSMD

        private void write_list_textures(string tofile)
        {
            StreamWriter text_file;
            FileStream text_filetr = new FileStream(tofile, FileMode.Create, FileAccess.Write);
            text_file = new StreamWriter(text_filetr);
            for (int i = 0; i < numMesh; i++)
            {
                text_file.WriteLine(_MeshData[i].name.Replace(" ", "_") + ".bmp\n");
            }
            text_file.Close();
        }
        private void Write_QC(string tofile, string modelname)
        {
            // test_transform();
            StreamWriter qcfile;
            FileStream QCStr = new FileStream(tofile, FileMode.Create, FileAccess.Write);
            qcfile = new StreamWriter(QCStr);
            qcfile.WriteLine("/*");
            qcfile.WriteLine("File QC - Duoc tao boi tools Convert LTB to SMD <Author:Giay Nhap>");
            qcfile.WriteLine("Fb:fb.com\\abcGiayNhapcba");
            qcfile.WriteLine("*/");
            qcfile.WriteLine("");
            qcfile.WriteLine("$modelname \"" + modelname + ".mdl\"");
            qcfile.WriteLine("$cd \"" + cur_Path + "\\\"");
            qcfile.WriteLine("$cdtexture \".\\\"");
            qcfile.WriteLine("$scale 1.0");
            qcfile.WriteLine("$cliptotextures");
            qcfile.WriteLine("");
            qcfile.WriteLine("$bbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$cbox 0.000000 0.000000 0.000000 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$eyeposition 0.000000 0.000000 0.000000");
            qcfile.WriteLine("//$origin 0.000000 0.000000 0.000000");
            qcfile.WriteLine("//$rotate 0.000000 0.000000 0.000000");
            qcfile.WriteLine("$scale 0.4");
            qcfile.WriteLine("");
            qcfile.WriteLine("$body \"gn_mesh\" \"" + modelname + "\" ");
            qcfile.WriteLine("");
            for (int i = 0; i < numAnim; i++)
            {
                qcfile.Write("$sequence \"" + _AnimData[i].name + "\" \"" + _AnimData[i].name + "\" fps " + _AnimData[i].listkeyframe[(int)_AnimData[i].nkeyframe - 1]);
                if (_AnimData[i].name.IndexOf("idle") != -1 || _AnimData[i].name.IndexOf("run") != -1) qcfile.WriteLine(" loop");
                else qcfile.WriteLine("");
            }

            qcfile.Close();
        }

        private void Write_SMD_MODEL(string tofile)
        {
            //   0962005339
            write_list_textures(tofile + "_LIST_TEX.txt");
            int MAXSTUDIOVERTS = 2000;
            StreamWriter smd;
            FileStream smdStr = new FileStream(tofile + ".smd", FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);
            write_model_header(smd);
            int num_f_brak = 0;
            int had_poly = 0;
            for (int i = 0; i < numMesh; i++)
            {

                if (i == 2 && is_slp_hand == true)
                {
                    smd.WriteLine("end");
                    smd.WriteLine("");
                    smd.Close();
                    smdStr = new FileStream(tofile + "_" + num_f_brak + ".smd", FileMode.Create, FileAccess.Write);
                    smd = new StreamWriter(smdStr);
                    write_model_header(smd);
                    num_f_brak += 1;
                }

                for (int j = 0; j < _MeshData[i].nIdx; j += 3)
                {
                    if (is_spl_model == true)
                    {
                        if ((j - had_poly + 3) / 3 > MAXSTUDIOVERTS)
                        {
                            had_poly += j;
                            smd.WriteLine("end");
                            smd.WriteLine("");
                            smd.Close();
                            smdStr = new FileStream(tofile + "_" + num_f_brak + ".smd", FileMode.Create, FileAccess.Write);
                            smd = new StreamWriter(smdStr);
                            write_model_header(smd);
                            num_f_brak += 1;
                        }
                    }
                    int tr = _MeshData[i].triangles[j];
                    int tr1 = _MeshData[i].triangles[j + 1];
                    int tr2 = _MeshData[i].triangles[j + 2];
                    smd.WriteLine(_MeshData[i].name.Replace(" ", "_") + ".bmp");
                    smd.WriteLine(_MeshData[i].weightsets_output[tr] + " " + _MeshData[i].vertices[tr][0].ToString("F6", culture) + " " + _MeshData[i].vertices[tr][1].ToString("F6", culture) + " " + _MeshData[i].vertices[tr][2].ToString("F6", culture) + " "
                    + _MeshData[i].normals[tr][0].ToString("F6", culture) + " " + _MeshData[i].normals[tr][1].ToString("F6", culture) + " " + _MeshData[i].normals[tr][2].ToString("F6", culture) + " " + _MeshData[i].uvs[tr][0].ToString("F6", culture) + " " + _MeshData[i].uvs[tr][1].ToString("F6", culture));
                    smd.WriteLine(_MeshData[i].weightsets_output[tr1] + " " + _MeshData[i].vertices[tr1][0].ToString("F6", culture) + " " + _MeshData[i].vertices[tr1][1].ToString("F6", culture) + " " + _MeshData[i].vertices[tr1][2].ToString("F6", culture) + " "
                   + _MeshData[i].normals[tr1][0].ToString("F6", culture) + " " + _MeshData[i].normals[tr1][1].ToString("F6", culture) + " " + _MeshData[i].normals[tr1][2].ToString("F6", culture) + " " + _MeshData[i].uvs[tr1][0].ToString("F6", culture) + " " + _MeshData[i].uvs[tr1][1].ToString("F6", culture));
                    smd.WriteLine(_MeshData[i].weightsets_output[tr2] + " " + _MeshData[i].vertices[tr2][0].ToString("F6", culture) + " " + _MeshData[i].vertices[tr2][1].ToString("F6", culture) + " " + _MeshData[i].vertices[tr2][2].ToString("F6", culture) + " "
                   + _MeshData[i].normals[tr2][0].ToString("F6", culture) + " " + _MeshData[i].normals[tr2][1].ToString("F6", culture) + " " + _MeshData[i].normals[tr2][2].ToString("F6", culture) + " " + _MeshData[i].uvs[tr2][0].ToString("F6", culture) + " " + _MeshData[i].uvs[tr2][1].ToString("F6", culture));

                }
            }
            smd.WriteLine("end");
            smd.WriteLine("");
            smd.Close();
        }
        private void Write_SMD_ANIM(int indexanim, string toFile)
        {
            StreamWriter smd;
            FileStream smdStr = new FileStream(toFile, FileMode.Create, FileAccess.Write);
            smd = new StreamWriter(smdStr);

            auto_calc_all_Frame(indexanim);

            smd.Write("version 1\nnodes\n");
            float gScan = 1.0f;
            for (int i = 0; i < numBones; i++)
            {
                smd.Write(" " + i + "  \"" + _BoneData[i].name + "\" " + _BoneData[i].par + "\n");
            }
            smd.Write("end\nskeleton\n");

            for (int i = 0; i < _AnimData[indexanim].nkeyframe; i++)
            {
                float giTime = (float)_AnimData[indexanim].listkeyframe[i] / gScan;
                if (float.IsNaN(giTime)) giTime = 0.0f;

                smd.Write("time " + Math.Round(giTime) + "\n");

                for (int k = 0; k < numBones; k++)
                {
                    double[] quaternion = new double[4] { _AnimData[indexanim].frame[k].quats[i][0], _AnimData[indexanim].frame[k].quats[i][1], _AnimData[indexanim].frame[k].quats[i][2], _AnimData[indexanim].frame[k].quats[i][3] };
                    double[,] matrix;


                    double length = Math.Sqrt(quaternion[0] * quaternion[0] + quaternion[1] * quaternion[1] + quaternion[2] * quaternion[2] + quaternion[3] * quaternion[3]);
                    quaternion[0] /= length;
                    quaternion[1] /= length;
                    quaternion[2] /= length;
                    quaternion[3] /= length;

                    matrix = QuaternionMatrix(quaternion);
                    double[] rotation = new double[3];




                    rotation = rotationMatrixToEulerAngles(matrix);



                    //  rotation = quaternionToRotation(quaternion);
                    // rotation = quaternionToRotation(quaternion);

                    float[] position = new float[3];
                    position[0] = _AnimData[indexanim].frame[k].pos[i][0];
                    position[1] = _AnimData[indexanim].frame[k].pos[i][1];
                    position[2] = _AnimData[indexanim].frame[k].pos[i][2];
                    if (_BoneData[k].par == -1) rotation[0] -= Math.PI / 2.0f;
                    smd.Write(k + "   " + position[0].ToString("F6", culture) + " " + position[1].ToString("F6", culture) + " " + position[2].ToString("F6", culture) + " " + rotation[0].ToString("F6", culture) + " " + rotation[1].ToString("F6", culture) + " " + rotation[2].ToString("F6", culture) + "\n");
                }
            }
            smd.Write("end");
            smd.Close();
        }
        private void write_model_header(StreamWriter smd)
        {


            smd.WriteLine("version 1");
            smd.WriteLine("nodes");
            for (int i = 0; i < numBones; i++)
            {
                smd.WriteLine(i + " \"" + _BoneData[i].name + "\" " + _BoneData[i].par);
            }

            smd.WriteLine("end");
            smd.WriteLine("skeleton");
            smd.WriteLine("time 0");
            for (int i = 0; i < numBones; i++)
            {
                smd.WriteLine(i + " " + _BoneData[i].bone_data_out);
            }
            smd.WriteLine("end");
            smd.WriteLine("triangles");
        }

        //public bool Decompress_file(string inFile, string outFile)
        //{
        //    var input = new FileStream(inFile, FileMode.Open);
        //    var decoder = new Decoder();

        //    try
        //    {
        //        var output = new FileStream(outFile, FileMode.Create);

        //        int bufSize = 24576, count;
        //        byte[] buf = new byte[bufSize];
        //        while ((count = decoder.Read(buf, 0, bufSize)) > 0)
        //        {
        //            output.Write(buf, 0, count);
        //        }
        //        input.Close();
        //        output.Close();
        //    }
        //    catch
        //    {
        //        return false;
        //    }
        //    return true;

        //}
        #endregion

        #region ParseData
        private void Parse_mesh(BinaryReader gbStream, uint numMesh)
        {
            for (int i = 0; i < numMesh; i++)
            {
                string meshName = Read_string(gbStream);

                uint numSubmesh = gbStream.ReadUInt32();
                for (int j = 0; j < numSubmesh; j++)
                    gbStream.ReadSingle();
                gbStream.ReadUInt32(); gbStream.ReadUInt32();
                Parse_submesh(gbStream, numSubmesh, i, meshName);
            }
        }

        private void parse_skeleton(BinaryReader gbStream)
        {
            for (int n = 0; n < numBones; n++)
            {
                _BoneData[n].matdata = new double[4, 4];
                _BoneData[n].name = Read_string(gbStream);
                _BoneData[n].isbone = gbStream.ReadByte();
                _BoneData[n].num2 = _LTBFile.ReadUInt16();

                for (long i = 0; i < 4; i++)
                {
                    for (int j = 0; j < 4; j++)
                    {
                        _BoneData[n].matdata[i, j] = _LTBFile.ReadSingle();
                    }
                }
                _BoneData[n].nSubbone = _LTBFile.ReadUInt32();
            }
        }

        private void Parse_submesh(BinaryReader gbStream, uint numSubmesh, int imesh, string meshName)
        {
            for (int i = 0; i < numSubmesh; i++)
            {
                imesh = (int)totalmesh;


                _MeshData[imesh].name = meshName + "_" + i;
                _MeshData[imesh].uvs = new List<float[]>();
                _MeshData[imesh].normals = new List<float[]>();
                _MeshData[imesh].vertices = new List<float[]>();
                _MeshData[imesh].weights = new List<float[]>();
                _MeshData[imesh].triangles = new List<int>();
                _MeshData[imesh].weightsets = new List<int[]>();
                _MeshData[imesh].weightsets_output = new List<int>();
                gbStream.ReadUInt32();
                uint matNum = gbStream.ReadUInt32();
                gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32();
                gbStream.ReadByte();
                uint unk1 = gbStream.ReadUInt32();
                uint sectionSize = gbStream.ReadUInt32();

                if (sectionSize != 0)
                {
                    long start = gbStream.BaseStream.Position;
                    uint numVerts = gbStream.ReadUInt32();
                    uint numIdx = gbStream.ReadUInt32() * 3;
                    uint meshType = gbStream.ReadUInt32();
                    if (meshType > 20)
                    {
                        is__doing = false;
                        return;
                    }
                    _MeshData[imesh].nvertices = numVerts;
                    _MeshData[imesh].type = meshType;
                    _MeshData[imesh].nIdx = numIdx;
                    gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32(); gbStream.ReadUInt32();
                    uint a = 0;
                    if (unk1 == 4)
                    {
                        a = gbStream.ReadUInt32();
                        _MeshData[imesh].weightsets.Add(new int[1] { (int)a });
                    }
                    if (unk1 == 5)
                        a = gbStream.ReadUInt16();
                    Parse_vertices(gbStream, numVerts, (LTBMeshType)meshType, imesh);
                    for (int j = 0; j < numIdx; j++)
                    {
                        _MeshData[imesh].triangles.Add(_LTBFile.ReadUInt16());
                    }
                    if (unk1 == 5)
                    {
                        int numWeight = gbStream.ReadInt32();
                        for (long j = 0; j < numWeight; j++)
                        {
                            _MeshData[imesh].weightsets.Add(new int[7] { gbStream.ReadInt16(), gbStream.ReadInt16(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadSByte(), gbStream.ReadInt32() });
                        }
                    }
                    // gbStream.BaseStream.Position = ispos+remain;
                    long unk2 = gbStream.ReadByte();
                    gbStream.BaseStream.Position += unk2;
                }
                totalmesh += 1;
            }
        }

        private void Parse_vertices(BinaryReader gbStream, uint numVerts, LTBMeshType meshType, int imesh)
        {

            if (meshType == (LTBMeshType)3)
            {
                meshType = LTBMeshType.LTB_MESHTYPE_TWOEXTRAFLOAT;
            }
            Boolean IncludeWeights = false;
            uint SkipDataSize = 0;
            if (meshType == LTBMeshType.LTB_MESHTYPE_NOTSKINNED)
            {
                IncludeWeights = false;
            }
            else
                if (meshType == LTBMeshType.LTB_MESHTYPE_EXTRAFLOAT)
            {
                IncludeWeights = true;
                //SkipDataSize = sizeof(Single);
            }
            else
                    if ((meshType == LTBMeshType.LTB_MESHTYPE_SKINNED) ||
                        (meshType == LTBMeshType.LTB_MESHTYPE_SKINNEDALT))
            {
                IncludeWeights = true;
            }
            else
                        if (meshType == LTBMeshType.LTB_MESHTYPE_TWOEXTRAFLOAT)
            {
                IncludeWeights = true;
            }
            for (int i = 0; i < numVerts; i++)
            {
                _MeshData[imesh].vertices.Add(new float[3] { gbStream.ReadSingle(), gbStream.ReadSingle(), gbStream.ReadSingle() });

                if (IncludeWeights)
                {

                    float f1; f1 = gbStream.ReadSingle();
                    float f2 = 0.0f;

                    if (meshType != LTBMeshType.LTB_MESHTYPE_EXTRAFLOAT)
                        f2 = gbStream.ReadSingle();
                    else f2 = 1.0f - f1;

                    float f3 = 0.0f;
                    float f4 = 0.0f;
                    if (meshType != LTBMeshType.LTB_MESHTYPE_TWOEXTRAFLOAT && meshType != LTBMeshType.LTB_MESHTYPE_EXTRAFLOAT)
                        f3 = gbStream.ReadSingle();
                    else f3 = 1.0f - (f2 + f1);
                    f4 = 1.0f - (f1 + f2 + f3);
                    if (f4 < 0.0f) f4 = 0.0f;
                    _MeshData[imesh].weights.Add(new float[4] { f1, f2, f3, f4 });

                }
                gbStream.BaseStream.Position = gbStream.BaseStream.Position + (int)SkipDataSize;
                _MeshData[imesh].normals.Add(new float[3] { gbStream.ReadSingle(), gbStream.ReadSingle(), gbStream.ReadSingle() });
                float[] uv = new float[2] { gbStream.ReadSingle(), 1.0f - gbStream.ReadSingle() };
                if (uv[0] > 1.0f) uv[0] -= 1.0f;
                _MeshData[imesh].uvs.Add(uv);
            }
        }

        private void Parse_animation(BinaryReader gbStream)
        {
            int nskipdata = gbStream.ReadInt32();
            long skipzie = 0; ;
            for (int k = 0; k < nskipdata; k++)
            {

                Read_string(_LTBFile);
                skipzie = gbStream.ReadUInt32();
                gbStream.BaseStream.Position += skipzie * 4;
            }

            UInt16 CompAnim = gbStream.ReadUInt16();
            UInt32 CompAnim2 = gbStream.ReadUInt16();

            numAnim = gbStream.ReadUInt16();
            _AnimData = new List<CAnimData>();
            gbStream.ReadUInt16();

            for (int i = 0; i < numAnim; i++)
            {
                // _AnimData[i].listkeyframe.Clear();
                _AnimData[i].Dim = new float[3];
                _AnimData[i].listkeyframe = new List<int>();
                _AnimData[i].listsound = new List<string>();
                _AnimData[i].frame = new CFramedata[numBones];
                _AnimData[i].Dim[0] = gbStream.ReadSingle();
                _AnimData[i].Dim[1] = gbStream.ReadSingle();
                _AnimData[i].Dim[2] = gbStream.ReadSingle();
                _AnimData[i].name = Read_string(gbStream);
                gbStream.ReadUInt32();
                _AnimData[i].interp_time = (int)gbStream.ReadUInt32();
                _AnimData[i].nkeyframe = gbStream.ReadUInt32();
                for (int j = 0; j < _AnimData[i].nkeyframe; j++)
                {
                    _AnimData[i].listkeyframe.Add((int)gbStream.ReadUInt32());
                    _AnimData[i].listsound.Add(Read_string(gbStream));

                }

                int nsup = gbStream.ReadInt16();
                Boolean first = false;
                // if (nsup != 0) MessageBox.Show(nsup+"");
                for (int k = 0; k < numBones; k++)
                {
                    _AnimData[i].frame[k].pos = new List<float[]>();
                    _AnimData[i].frame[k].quats = new List<float[]>();

                    if (nsup != 0)
                    {
                        if (first == false)
                        {
                            first = true;
                            gbStream.BaseStream.Position -= 2;
                        }

                        int gframe_2;
                        int gframe_1;
                        gframe_1 = gbStream.ReadInt16();

                        gbStream.ReadInt16();
                        float[] p = new float[3];
                        float[] q = new float[4];
                        for (int j = 0; j < gframe_1; j++)
                        {
                            p = new float[3];
                            p[0] = UnpackFromInt16(gbStream.ReadInt16());
                            p[1] = UnpackFromInt16(gbStream.ReadInt16());
                            p[2] = UnpackFromInt16(gbStream.ReadInt16());
                            _AnimData[i].frame[k].pos.Add(p);

                        }
                        if (gframe_1 < _AnimData[i].nkeyframe)
                            for (int j = gframe_1; j < _AnimData[i].nkeyframe; j++)
                            {
                                _AnimData[i].frame[k].pos.Add(p);
                            }
                        gframe_2 = gbStream.ReadInt16();
                        gbStream.ReadInt16();
                        for (int j = 0; j < gframe_2; j++)
                        {
                            q = new float[4];
                            q[0] = gbStream.ReadUInt16() / 32767.0f;
                            q[1] = gbStream.ReadUInt16() / 32767.0f;
                            q[2] = gbStream.ReadUInt16() / 32767.0f;
                            q[3] = gbStream.ReadUInt16() / 32767.0f;
                            _AnimData[i].frame[k].quats.Add(q);
                        }
                        if (gframe_2 < _AnimData[i].nkeyframe)
                            for (int j = gframe_2; j < _AnimData[i].nkeyframe; j++)
                            {
                                _AnimData[i].frame[k].quats.Add(q);
                            }

                    }
                    else
                    {

                        if (k >= 2)
                        {
                            gbStream.ReadByte();
                        }

                        for (int j = 0; j < _AnimData[i].nkeyframe; j++)
                        {

                            float[] p = new float[3];
                            if (k == 0)
                            {

                                p[0] = UnpackFromInt16((Int16)_LTBFile.ReadUInt16());
                                _LTBFile.ReadUInt16();
                                p[1] = UnpackFromInt16((Int16)_LTBFile.ReadUInt16());
                                _LTBFile.ReadUInt16();
                                p[2] = UnpackFromInt16((Int16)_LTBFile.ReadUInt16());
                                _LTBFile.ReadUInt16();


                            }
                            else
                            {

                                p[0] = _LTBFile.ReadSingle();
                                p[1] = _LTBFile.ReadSingle();
                                p[2] = _LTBFile.ReadSingle();

                            }

                            _AnimData[i].frame[k].pos.Add(p);

                        }
                        for (int j = 0; j < _AnimData[i].nkeyframe; j++)
                        {
                            float[] q = new float[4];
                            if (k == 0)
                            {
                                q[0] = (float)_LTBFile.ReadUInt16() / 32767.0f;
                                _LTBFile.ReadInt16();
                                q[1] = (float)_LTBFile.ReadUInt16() / 32767.0f;
                                _LTBFile.ReadInt16();
                                q[2] = (float)_LTBFile.ReadUInt16() / 32767.0f;
                                _LTBFile.ReadInt16();
                                q[3] = (float)_LTBFile.ReadUInt16() / 32767.0f;
                                _LTBFile.ReadInt16();
                            }
                            else
                            {
                                q[0] = _LTBFile.ReadSingle();
                                q[1] = _LTBFile.ReadSingle();
                                q[2] = _LTBFile.ReadSingle();
                                q[3] = _LTBFile.ReadSingle();
                            }
                            _AnimData[i].frame[k].quats.Add(q);

                        }
                    }
                }
            }


        }

        private string Read_string(BinaryReader gbStream)
        {
            string returndata = "";
            UInt16 nChar = gbStream.ReadUInt16();
            for (int n = 0; n < nChar; n++)
                returndata += gbStream.ReadChar();
            return returndata;
        }

        private float UnpackFromInt16(Int16 intval)
        {
            return (float)(intval) * NKF_TRANS_OOSCALE_1_11_4;
        }

        private int GetBoneWeightD(int[] num, float[] size)
        {
            float max = size[0];
            int boneWeight = num[1];
            if (num[0] > 4) num[0] = 4;
            for (int i = 1; i < num[0]; i++)
            {
                if (max < size[i])
                {
                    max = size[i];
                    boneWeight = num[i + 1];
                }
            }
            return boneWeight;
        }
        private bool Decompress_file(string inFile, string outFile)
        {
            try
            {
                // 打开输入文件（压缩文件）
                using (FileStream input = new FileStream(inFile, FileMode.Open))
                {
                    // 创建 LZMA 解码器
                    Decoder decoder = new Decoder();

                    // 读取 LZMA 属性头（5 字节）
                    byte[] properties = new byte[5];
                    if (input.Read(properties, 0, 5) != 5)
                    {
                        throw new Exception("输入文件已损坏：无法读取 LZMA 属性头。");
                    }

                    // 读取未压缩文件的大小（8 字节）
                    byte[] fileLengthBytes = new byte[8];
                    if (input.Read(fileLengthBytes, 0, 8) != 8)
                    {
                        throw new Exception("输入文件已损坏：无法读取未压缩文件大小。");
                    }
                    long fileLength = BitConverter.ToInt64(fileLengthBytes, 0);

                    // 设置解码器属性
                    decoder.SetDecoderProperties(properties);

                    // 创建输出文件（解压后的文件）
                    using (FileStream output = new FileStream(outFile, FileMode.Create))
                    {
                        // 解压数据
                        decoder.Code(input, output, input.Length, fileLength, null);
                    }
                }

                Console.WriteLine("文件解压完成！");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("解压失败：" + ex.Message);
                return false;
            }
        }

        #endregion

        private uint totalmesh = 0;
        private int Scaleto = 255;
        private uint numMesh = 0;
        private uint numBones = 0;
        private uint numAnim = 0;
        private bool isAnim = false;
        private bool isAutoScaler = false;
        private bool isSubForm = false;
        private bool isCalcframe = true;
        private bool isAutoCreateQC = false;
        private bool isCheckboxAnim = true;
        private bool is__doing;
        private bool is_slp_hand = false;
        private bool is_spl_model = false;
        private const float NKF_TRANS_SCALE_1_11_4 = (16.0f);		// 2^4
        private const float NKF_TRANS_OOSCALE_1_11_4 = (1.0f / NKF_TRANS_SCALE_1_11_4);

        private string cur_fName = "", cur_Path = "";
        private static List<double[,]> Matrix4x4s = new List<double[,]>();
        private CultureInfo? culture;

        private BinaryReader? _LTBFile;
        private List<CMeshData>? _MeshData;
        private List<CBoneData>? _BoneData;
        private List<CAnimData>? _AnimData;
        private List<string>? _InputFiles;
        private List<string>? _InputFileList;
        enum LTBMeshType
        {
            LTB_MESHTYPE_NOTSKINNED = 1,
            LTB_MESHTYPE_EXTRAFLOAT,
            LTB_MESHTYPE_SKINNED,
            LTB_MESHTYPE_SKINNEDALT,
            LTB_MESHTYPE_TWOEXTRAFLOAT
        };
        private class CMeshData
        {
            public string? name;
            public uint nvertices;
            public uint nIdx;
            public List<float[]>? vertices;
            public List<float[]>? normals;
            public List<float[]>? uvs;
            public List<float[]>? weights;
            public List<int[]>? weightsets;
            public List<int>? weightsets_output;
            public List<int>? triangles;
            public uint type;
        };
        private class CBoneData
        {
            public string? name;
            public uint nSubbone;
            public double[,]? matdata;
            public string? bone_data_out;
            public uint isbone;
            public uint num2;
            public int par;
        };
        private class CFramedata
        {
            int indexframe;
            public List<float[]>? pos;
            public List<float[]>? quats;
        };
        private class CAnimData
        {
            public string? name;
            public uint nkeyframe;
            public List<int>? listkeyframe;
            public List<string>? listsound;
            public float[]? Dim;
            public int interp_time;
            public CFramedata[] frame;
        };
    }
}
