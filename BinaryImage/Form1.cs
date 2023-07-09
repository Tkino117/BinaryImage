using System;
using System.IO;
using System.Text;

namespace BinaryImage
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //ヘッダー情報
        byte[] header;
        int offset;
        int width;
        int height;

        //入力画像を格納
        byte[] imginput_byte;
        byte[,,] imginput_rgb; //rgbで格納
        float[,] imginput_Y;   //Yで格納

        //本体
        private void buttonLoad_Click(object sender, EventArgs e)
        {
            loadBmp();
            
            //処理用の輝度のみ二次元配列
            float[,] process_Y = copy(imginput_Y);

            /*
            process_Y = binary_block(process_Y, 15, 7f, 11f);
            process_Y = denoise_black_binary(process_Y, 100, 2);
            */

            // これ好き
            /*
            process_Y = gauss(process_Y, 3, 0.5f);
            process_Y = binary_block(process_Y, 20, 5.6f, 11f);

            process_Y = denoise_black_binary(process_Y, 100, 2);
            
            //process_Y = denoise_white_binary(process_Y, 100, 1);
            */

            // 事前ノイズ処理ばーじょん
            process_Y = gauss_sugoi(process_Y, 5, 1.7f, 17);        //ノイズ処理
            process_Y = binary_block(process_Y, 20, 5.6f, 10f);     //二値化
            process_Y = denoise_black_binary(process_Y, 100, 3);    //小さなノイズ除去

            //出力
            outBmp(Y_To_Rgb(process_Y), "output.bmp");

            this.Close();
        }

        //BMPを入力
        private void loadBmp()
        {
            //ファイル全体を取得
            string path = textBoxPath.Text;
            imginput_byte = File.ReadAllBytes(path);

            //先頭から画像データまでのオフセット
            byte[] bitOffset = { imginput_byte[10], 0, 0, 0 };
            offset = BitConverter.ToInt32(bitOffset);

            //幅と高さを取得
            byte[] byteWidth = imginput_byte.Skip(18).Take(4).ToArray();  //幅をビットで取得
            byte[] byteHeight = imginput_byte.Skip(22).Take(4).ToArray(); //高さをビットで取得
            width = BitConverter.ToInt32(byteWidth);
            height = BitConverter.ToInt32(byteHeight);

            //headerを取得
            header = imginput_byte.Take(offset).ToArray();

            //画像データを取得しrgb配列に代入
            imginput_rgb = new byte[3, width, height];
            int l =  offset; //取得する地点
            for(int i = 0; i < height; i++)    //縦ループ（下から上）
            {
                for (int j = 0; j < width; j++) //横ループ（左から右）
                {
                    for (int k = 0; k < 3; k++)         //B G R
                    {
                        imginput_rgb[k, j, i] = imginput_byte[l];
                        l++;
                    }
                }
            } 

            //Yデータを格納
            imginput_Y = rgb_To_Y(imginput_rgb);
        }

        //BMPを出力（引数：RGB配列）
        private void outBmp(byte[,,] imgin_RGB, string fileName)
        {
            byte[] output = imginput_byte;

            int l = offset;
            for(int i = 0;i < height; i++)
            {
                for(int j = 0;j < width; j++)
                {
                    for(int k = 0;k < 3; k++)
                    {
                        output[l] = imgin_RGB[k, j, i];
                        l++;
                    }
                }
            }
            //string path = @"E:\output\" + fileName;
            string path = @"G:\マイドライブ\02_Prorjects\01_BinaryImage\BinaryImage\BinaryImage\Sample\" + fileName;
            //string path = "E:\\07_C#Projects\\04_GaussBlur\\BinaryImage\\BinaryImage\\Sample\\output.bmp";
            File.WriteAllBytes(path, output);
        }

        //rgbをyに（引数：RGB配列）
        private float[,] rgb_To_Y(byte[,,] imgin_RGB)
        {
            //返すY配列
            float[,] imgout = new float[width, height];

            //imgin(rgb)をimgout(Y)に
            byte b = 3;
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    imgout[j, i] = 0.299f * imgin_RGB[2, j, i] + 0.587f * imgin_RGB[1, j, i] + 0.114f * imgin_RGB[0, j, i];
                }
            }

            return imgout;
        }

        //yをrgbに（引数：Y配列）
        private byte[,,] Y_To_Rgb(float[,] imgin_Y)
        {
            //返すRGB配列
            byte[,,] imgout = new byte[3, width, height];

            //imgin(Y)をimgout(rgb)に
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    imgout[0, j, i] = (byte)imgin_Y[j, i];
                    imgout[1, j, i] = (byte)imgin_Y[j, i];
                    imgout[2, j, i] = (byte)imgin_Y[j, i];
                }
            }

            return imgout;
        }

        //配列をコピーするだけ
        private float[,] copy(float[,] input)
        {
            float[,] output = new float[input.GetLength(0), input.GetLength(1)];
            for (int x = 0; x < input.GetLength(0); x++)
            {
                for(int y = 0; y < input.GetLength(1); y++) 
                {
                    output[x, y] = input[x, y];
                }
            }
            return output;
        }

        //ガウス分布を返す関数、sigma=0を入れると一様な分布を返す，size:1→3x3，和が1になるように補正あり
        private float[,] gaussAry(int size, float sigma)
        {
            //行列を生成
            float[,] gaussary = new float[size * 2 + 1, size * 2 + 1];

            //ガウス分布（sigma!=0)、一様な分布（sigma=0）
            if (sigma != 0)
            {
                float tmp_sum = 0; //値の合計値
                float pi = (float)Math.PI;
                for (int i = 0; i < size * 2 + 1; i++)
                {
                    for (int j = 0; j < size * 2 + 1; j++)
                    {
                        gaussary[i, j] = (float)((1 / (2 * pi * Math.Pow(sigma, 2))) * Math.Exp((Math.Pow(i - size, 2) + Math.Pow(j - size, 2)) / (-2 * Math.Pow(sigma, 2))));

                        tmp_sum += gaussary[i, j];
                    }
                }

                //和が1になるように補正
                float tmp_sum2 = 0; //値の合計値2
                float error_range = 0.03f;         //この値より離れていたら警告
                float correct_rate_upper = 1.1f;   //補正倍率の上限値
                float correct_rate_lower = 0.9f;   //補正倍率の下限値
                float correct_rate = 1 / tmp_sum;  //補正倍率

                //補正倍率が範囲内でないとき，警告（一応）
                //if (correct_rate < 1 - error_range || 1 + error_range < correct_rate)
                    //MessageBox.Show("サイズ" + size + "のガウス分布行列の合計値が" + tmp_sum + "です\r（警告）");


                correct_rate = Math.Max(Math.Min(correct_rate, correct_rate_upper), correct_rate_lower);  //補正倍率を上限下限内に

                //倍率で補正
                for (int i = 0; i < size * 2 + 1; i++)
                {
                    for (int j = 0; j < size * 2 + 1; j++)
                    {
                        gaussary[i, j] = correct_rate * gaussary[i, j];
                        tmp_sum2 += gaussary[i, j];
                    }
                }
                //中央の値で補正（中央を全体-その他で計算）
                gaussary[size, size] += 1 - tmp_sum2;
            }
            else
            {
                for (int i = 0; i < size * 2 + 1; i++)
                {
                    for (int j = 0; j < size * 2 + 1; j++)
                    {
                        gaussary[i, j] = (float)(1 / Math.Pow(size * 2 + 1, 2));
                    }
                }
            }


            return gaussary;
        }

        //ガウスぼかし sigma=1.7がとりあえずおすすめ
        private float[,] gauss(float[,] imgin_Y, int blocksize, float sigma)
        {
            //inとout用のYデータを定義
            float[,] imgbefore = copy(imgin_Y);
            float[,] imgafter = new float[width, height];

            //ガウス分布の行列を生成
            float[,] filter_gauss = gaussAry(blocksize, sigma);

            float tmp_gauss = 0f;  //フィルタ内の明るさの合計（imgY）
            int posX;      //sum時に参照する座標X
            int posY;      //sum時に参照する座標Y
            
            
            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    //ガウスぼかし
                    for (int y2 = -blocksize; y2 <= blocksize; y2++)
                    {
                        for (int x2 = -blocksize; x2 <= blocksize; x2++)
                        {
                            //参照座標と鏡像変換
                            posX = x1 + x2;
                            posY = y1 + y2;
                            posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                            posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);

                            //参照した座標の値×ガウスの係数をtmp_gaussに加える
                            tmp_gauss += imgbefore[posX, posY] * filter_gauss[x2 + blocksize, y2 + blocksize];
                        }
                    }

                    //tmp_gaussの合計値をimgafterに繁栄
                    imgafter[x1, y1] = tmp_gauss;

                    //いろいろリセットする
                    tmp_gauss = 0f;
                }
            }

            //imgafterを返す
            return imgafter;
        }

        //ノイズ処理（自身と近い値のみガウスぼかし） sigma=1.7がおすすめ？ differenceは拾う輝度差
        private float[,] gauss_sugoi(float[,] imgin_Y, int blocksize, float sigma, float difference)
        {
            //inとout用のYデータを定義
            float[,] imgbefore = copy(imgin_Y);
            float[,] imgafter = copy(imgin_Y);

            //imgafterの初期値
            for (int i = 0; i < height; i++)
            {
                for (int j = 0; j < width; j++)
                {
                    imgafter[j, i] = 0f;
                }
            }

            //ガウス分布の行列を生成
            float[,] filter_gauss = gaussAry(blocksize, sigma);
            float[,] tmp_filter_gauss = copy(filter_gauss);   //処理用のフィルタ

            float tmp_gauss = 0f;  //フィルタ内の明るさの合計（imgY）
            float tmp_gauss_sum = 0f;
            float tmp_gauss_correctRate = 0f;
            int posX;      //sum時に参照する座標X
            int posY;      //sum時に参照する座標Y

            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    //処理用のガウス行列をつくる（自分と違う色の場所は0）
                    for (int y2 = -blocksize; y2 <= blocksize; y2++)
                    {
                        for (int x2 = -blocksize; x2 <= blocksize; x2++)
                        {
                            //参照座標と鏡像変換
                            posX = x1 + x2;
                            posY = y1 + y2;
                            posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                            posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);

                            //参照した座標の値と自身の差が大きい場合，行列の値は0
                            if (Math.Abs(imgbefore[posX, posY] - imgbefore[x1, y1]) > difference)
                            {
                                tmp_filter_gauss[x2 + blocksize, y2 + blocksize] = 0;
                            }

                            //行列の合計値に加算
                            tmp_gauss_sum += tmp_filter_gauss[x2 + blocksize, y2 + blocksize];
                        }
                    }

                    //ガウス行列の和を1にする
                    tmp_gauss_correctRate = 1 / tmp_gauss_sum;
                    for (int i = 0; i < blocksize * 2 + 1; i++)
                    {
                        for (int j = 0; j < blocksize * 2 + 1; j++)
                        {
                            tmp_filter_gauss[i, j] *= tmp_gauss_correctRate;
                        }
                    }

                    //リセット
                    tmp_gauss_sum = 0f;

                    //ガウスぼかし
                    for (int y2 = -blocksize; y2 <= blocksize; y2++)
                    {
                        for (int x2 = -blocksize; x2 <= blocksize; x2++)
                        {
                            //参照座標と鏡像変換
                            posX = x1 + x2;
                            posY = y1 + y2;
                            posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                            posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);

                            //参照した座標の値×ガウスの係数をtmp_gaussに加える
                            tmp_gauss += imgbefore[posX, posY] * tmp_filter_gauss[x2 + blocksize, y2 + blocksize];
                        }
                    }

                    //tmp_gaussの合計値をimgafterに繁栄
                    imgafter[x1, y1] = tmp_gauss;

                    //いろいろリセットする
                    tmp_gauss = 0f;
                    tmp_filter_gauss = copy(filter_gauss);
                }
            }

            //imgafterを返す
            return imgafter;
        }

        //ブロック内の平均と比較して二値画像を返す（sigmaは重みづけ，sigma=0で一葉な平均，thresholdは白黒を決定する閾値の差分 +で白が増える）
        private float[,] binary_block(float[,] imgin_Y, int blocksize, float sigma, float threshold)
        {
            float[,] imgout = new float[width, height];
            float[,] gauss_ary = gaussAry(blocksize, sigma);
            int posX, posY;
            float tmp_aveY = 0;
            for (int x1 = 0; x1 < width; x1++)
            {
                for (int y1 = 0; y1 < height; y1++)
                {
                    for (int x2 = -blocksize; x2 <= blocksize; x2++)
                    {
                        for (int y2 = -blocksize; y2 <= blocksize; y2++)
                        {
                            //鏡像変換
                            posX = x1 + x2; posY = y1 + y2;
                            posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                            posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);

                            //tmp_aveYに輝度×ガウス分布を加算
                            tmp_aveY += imgin_Y[posX, posY] * gauss_ary[x2 + blocksize, y2 + blocksize];
                        }
                    }

                    //自身の白黒を決定
                    if (tmp_aveY - imgin_Y[x1, y1] >= threshold)
                        imgout[x1, y1] = 0;
                    else
                        imgout[x1, y1] = 255;

                    //リセット
                    tmp_aveY = 0;
                }
            }

            //返す
            return imgout;
        }

        //ちゃんとした二値画像のノイズ除去（黒）（binarythreshold:白黒の閾値、noisesize:ノイズと判定される最大サイズ）
        private float[,] denoise_black_binary(float[,] imgin_Ybinary, int binarythreshold, int noisesize)
        {
            float[,] imgout = new float[width, height];
            int[,] imgprosess = new int[width, height];  //処理用，0か1しかいれない

            //入力imageを0, 1の二値に
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (imgin_Ybinary[x, y] >= binarythreshold)
                        imgprosess[x, y] = 1;
                    else
                        imgprosess[x, y] = 0;
                }
            }

            //黒点を発見したら，その点と近辺(3x3)の黒点をlistBlack，listExploredに格納，listExploredの中身を順に探索
            int exX, exY;               //探索する中心座標
            int posX, posY;             //参照する座標
            int order;                  //orderは(x, y)に固有の割り当て番号（左下から順に0,1,2..)
            var listBlack = new List<int>();    //基準点周りの黒点のorderを格納
            var listExplored = new List<int>(); //探索済みの黒点のorderを全て格納
            int currentOrder_of_list = 0;       //現在探索している黒点のlistExploredにおける順番(何番目か)
            int tmpBlackSize = 0;  //黒点の集まりのサイズ

            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    order = xy_to_order(x1, y1);

                    //はじめて発見した黒点だった場合，リストに点を追加して探索を開始
                    if (imgprosess[x1, y1] == 0 && !listExplored.Contains(order)) {
                        listExplored.Add(order);
                        listBlack.Add(order);
                        tmpBlackSize = 1;
                        exX = x1; exY = y1; //次の探索座標

                        while (true)
                        {
                            for (int y2 = -1; y2 <= 1; y2++)
                            {
                                for (int x2 = -1; x2 <= 1; x2++)
                                {
                                    //参照座標と鏡像変換
                                    posX = exX + x2; posY = exY + y2;
                                    posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                                    posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);
                                    order = xy_to_order(posX, posY);

                                    //[posX, posY]が初めて発見した黒点のとき
                                    if (imgprosess[posX, posY] == 0 && !listExplored.Contains(order))
                                    {
                                        listExplored.Add(order);
                                        listBlack.Add(order);
                                        tmpBlackSize++;
                                    }
                                }
                            }

                            //周辺8マスを探索し終えたら次に探索する座標（あれば）を指定
                            if (listExplored.Count - 1 > currentOrder_of_list)
                            {
                                currentOrder_of_list++;
                                exX = order_to_x(listExplored[currentOrder_of_list]);
                                exY = order_to_y(listExplored[currentOrder_of_list]);
                            }
                            else
                            {
                                //次に探索する座標が無かった場合黒点の集まりのサイズが決定される
                                //一定数より小さかった場合ノイズ扱いとして白くする
                                if (tmpBlackSize <= noisesize)
                                {
                                    int x, y;
                                    foreach (int orderBlack in listBlack)
                                    {
                                        x = order_to_x(orderBlack);
                                        y = order_to_y(orderBlack);
                                        imgprosess[x, y] = 1;
                                    }
                                }

                                //いろいろリセットしてwhileから抜け出す
                                listBlack.Clear();
                                tmpBlackSize = 0;
                                break;
                            }
                        }
                    }
                }
            }

            //0, 1の二値を0, 255にして返す
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    imgout[x, y] = imgprosess[x, y] * 255;
                }
            }

            return imgout;
        }

        //ちゃんとした二値画像ノイズ除去（白）（binarythreshold:白黒の閾値、noisesize:ノイズと判定される最大サイズ）
        private float[,] denoise_white_binary(float[,] imgin_Ybinary, int binarythreshold, int noisesize)
        {
            float[,] imgout = new float[width, height];
            int[,] imgprosess = new int[width, height];  //処理用，0か1しかいれない

            //入力imageを0, 1の二値に
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (imgin_Ybinary[x, y] >= binarythreshold)
                        imgprosess[x, y] = 1;
                    else
                        imgprosess[x, y] = 0;
                }
            }

            //白点を発見したら，その点と近辺(3x3)の黒点をlistBlack，listExploredに格納，listExploredの中身を順に探索
            int exX, exY;               //探索する中心座標
            int posX, posY;             //参照する座標
            int order;                  //orderは(x, y)に固有の割り当て番号（左下から順に0,1,2..)
            var listWhite = new List<int>();    //基準点周りの黒点のorderを格納
            var listExplored = new List<int>(); //探索済みの黒点のorderを格納
            int currentOrder_of_list = 0;       //現在探索している白点のlistExploredでの順番(何番目か)
            int tmpWhiteSize = 0;  //白点の集まりのサイズ

            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    order = xy_to_order(x1, y1);

                    //はじめて発見した白点だった場合，リストに点を追加して探索を開始
                    if (imgprosess[x1, y1] == 1 && !listExplored.Contains(order))
                    {
                        listExplored.Add(order);
                        listWhite.Add(order);
                        tmpWhiteSize = 1;
                        exX = x1; exY = y1; //次の探索座標

                        while (true)
                        {
                            for (int y2 = -1; y2 <= 1; y2++)
                            {
                                for (int x2 = -1; x2 <= 1; x2++)
                                {
                                    //参照座標と鏡像変換
                                    posX = exX + x2; posY = exY + y2;
                                    posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                                    posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);
                                    order = xy_to_order(posX, posY);

                                    //[posX, posY]が初めて発見した白点のとき
                                    if (imgprosess[posX, posY] == 1 && !listExplored.Contains(order))
                                    {
                                        listExplored.Add(order);
                                        listWhite.Add(order);
                                        tmpWhiteSize++;
                                    }
                                }
                            }

                            //周辺8マスを探索し終えたら次に探索する座標（あれば）を指定
                            if (listExplored.Count - 1 > currentOrder_of_list)
                            {
                                currentOrder_of_list++;
                                exX = order_to_x(listExplored[currentOrder_of_list]);
                                exY = order_to_y(listExplored[currentOrder_of_list]);
                            }
                            else
                            {
                                //次に探索する座標が無かった場合白点の集まりのサイズが決定される
                                //一定数より小さかった場合ノイズ扱いとして黒くする
                                if (tmpWhiteSize <= noisesize)
                                {
                                    int x, y;
                                    foreach (int orderWhite in listWhite)
                                    {
                                        x = order_to_x(orderWhite);
                                        y = order_to_y(orderWhite);
                                        imgprosess[x, y] = 0;
                                    }
                                }

                                //いろいろリセットしてwhileから抜け出す
                                listWhite.Clear();
                                tmpWhiteSize = 0;
                                break;
                            }
                        }
                    }
                }
            }

            //0, 1の二値を0, 255にして返す
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    imgout[x, y] = imgprosess[x, y] * 255;
                }
            }

            return imgout;
        }

        //denoise_xxx_binaryのための関数
        private int xy_to_order(int x, int y)
        {
            return y * width + x;
        }
        private int order_to_x(int order)
        {
            return order % width;
        }
        private int order_to_y(int order)
        {
            return order / height;
        }


        //---------------（ここより下は遺物、動きはする）-------------------------

        //（使ってない）
        // 周りの3x3を探索し配列化
        //  1  3 2 1
        //  0  4   0
        // -1  5 6 7
        //    -1 0 1  という順序．i ↔ (x,y)の変換はxy_to_i()とi_to_xy()で
        private int xy_to_i(int x, int y)
        {
            return (-y + 2 * Math.Abs(y) - 2) * x - 4 * Math.Sign(y + 1) + 6;
        }
        private int[] i_to_xy(int i)
        {
            int[] xy = new int[2];
            xy[0] = Math.Sign(Math.Abs(i - 4) - 2);
            xy[1] = Math.Sign(Math.Abs(i - 2) - 2) * (-1);
            return xy;
        }

        //（使ってない，ひどい）
        //1x1, 2x1のノイズ除去
        private float[,] denoise_binary_1x2(float[,] imgin_Ybinary, int binarythreshold)
        {
            float[,] imgbefore = copy(imgin_Ybinary);
            float[,] imgout = copy(imgin_Ybinary);

            //念のため二値化
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (imgbefore[x, y] >= binarythreshold)
                        imgbefore[x, y] = 255;
                    else
                        imgbefore[x, y] = 0;
                }
            }

            //ノイズ除去
            int posX, posY;
            float sum3x3 = 0, sum5x5 = 0;
            for (int x1 = 0; x1 < width; x1++)
            {
                for (int y1 = 0; y1 < height; y1++)
                {
                    for (int x2 = -2; x2 <= 2; x2++)
                    {
                        for (int y2 = -2; y2 <= 2; y2++)
                        {
                            //参照座標と鏡像変換
                            posX = x1 + x2; posY = y1 + y2;
                            posX = Math.Abs(posX) - 2 * Math.Max(0, posX - width + 1);
                            posY = Math.Abs(posY) - 2 * Math.Max(0, posY - height + 1);

                            if ((Math.Abs(x2) <= 1 && Math.Abs(y2) <= 1) && !(x2 == 0 && y2 == 0))
                                sum3x3 += imgbefore[posX, posY];
                            if (Math.Abs(x2) == 2 || Math.Abs(y2) == 2)
                                sum5x5 += imgbefore[posX, posY];
                        }
                    }

                    if (sum3x3 < 1 || (sum3x3 < 256 && sum5x5 < 256))
                    {
                        imgout[x1, y1] = 0;
                    }
                    else if (sum3x3 > 255 * 8 - 1 || (sum3x3 > 255 * 7 - 1 && sum5x5 > 255 * 7 - 1))
                    {
                        imgout[x1, y1] = 255;
                    }

                    //リセット
                    sum3x3 = 0;
                    sum5x5 = 0;
                }
            }

            return imgout;
        }

        //（使ってない）
        //となりとの差の和を返す関数（引数：Y配列 / 戻り値の最大値：255）
        private float[,] differenceRootSumSqArray(float[,] imgin_Y)
        {
            //返す配列
            float[,] imgout = new float[width, height];

            int posX1 = 0, posX2 = 0, posY1 = 0, posY2 = 0;
            float difX1 = 0f, difX2 = 0f, difY1 = 0f, difY2 = 0f;
            float difSum;
            //float difSqRoot = 0;
            for(int y = 0; y < height; y++)
            {
                for(int x = 0; x < width; x++)
                {
                    //参照する座標と鏡像変換
                    if (x == 0)
                        posX2 = x + 1;
                    else
                        posX2 = x - 1;
                    if (x == width - 1)
                        posX1 = x - 1;
                    else
                        posX1 = x + 1;
                    if (y == 0)
                        posY1 = y + 1;
                    else
                        posY1 = y - 1;
                    if (y == height - 1)
                        posY2 = y - 1;
                    else
                        posY2 = y + 1;

                    difX1 = imgin_Y[posX1, y] - imgin_Y[x, y];
                    difX2 = imgin_Y[posX2, y] - imgin_Y[x, y];
                    difY1 = imgin_Y[x, posY1] - imgin_Y[x, y];
                    difY2 = imgin_Y[x, posY2] - imgin_Y[x, y];

                    difSum = (float)(Math.Abs(difX1) + Math.Abs(difX2) + Math.Abs(difY1) + Math.Abs(difY2));
                    imgout[x, y] = difSum / 4 * 4;

                    //これは二乗和平方根（不採用、もしやるなら平均の２乗をひかなきゃ）
                    //difSqRoot = (float)(Math.Sqrt(Math.Pow(difX1, 2) + Math.Pow(difX2, 2) + Math.Pow(difY1, 2) + Math.Pow(difY2, 2)));
                    //imgout[x, y] = difSqRoot / 4;
                }
            }

            //返す
            return imgout;
        }

        //（使ってない）
        //エントロピー（仮名）を返す関数（引数：Y配列 / 戻り値の最大値：255）
        private float[,] entropyArray(float[,] imgin_Y, int filterRange, bool useNormDist, float sigma)
        {
            //返す配列
            float[,] imgout = new float[width, height];

            //フィルタサイズのガウス分布を生成
            float[,] gaussary = new float[filterRange * 2 + 1, filterRange * 2 + 1];
            if (useNormDist)
                gaussary = gaussAry(filterRange, sigma);
            else
                gaussary = gaussAry(filterRange, 0);

            int[] pos = new int[6];      //0:x 1:x++ 2:x-- 3:y 4:y++ 5:y--の位置情報を格納
            float difSum = 0f;
            //float difSq = 0f, difSqRootAve = 0f;            //差のニ乗和、二乗和平方根平均を格納
            float entropy = 0f;
            for (int y1 = 0; y1 < height; y1++)
            {
                for (int x1 = 0; x1 < width; x1++)
                {
                    for(int y2 = -filterRange; y2 <= filterRange; y2++)
                    {
                        for (int x2 = -filterRange; x2 <= filterRange; x2++)
                        {
                            pos[0] = x1 + x2; pos[1] = x1 + x2 + 1; pos[2] = x1 + x2 - 1; pos[3] = y1 + y2; pos[4] = y1 + y2 + 1; pos[5] = y1 + y2 - 1;

                            //鏡像変換
                            pos[0] = Math.Abs(pos[0]) - 2 * Math.Max(0, pos[0] - (width - 1));
                            pos[1] = Math.Abs(pos[1]) - 2 * Math.Max(0, pos[1] - (width - 1));
                            pos[2] = Math.Abs(pos[2]) - 2 * Math.Max(0, pos[2] - (width - 1));
                            pos[3] = Math.Abs(pos[3]) - 2 * Math.Max(0, pos[3] - (height - 1));
                            pos[4] = Math.Abs(pos[4]) - 2 * Math.Max(0, pos[4] - (height - 1));
                            pos[5] = Math.Abs(pos[5]) - 2 * Math.Max(0, pos[5] - (height - 1));

                            //自身との差のを加算
                            difSum += (float)Math.Abs(imgin_Y[pos[1], pos[3]] - imgin_Y[pos[0], pos[3]]);
                            difSum += (float)Math.Abs(imgin_Y[pos[2], pos[3]] - imgin_Y[pos[0], pos[3]]);
                            difSum += (float)Math.Abs(imgin_Y[pos[0], pos[4]] - imgin_Y[pos[0], pos[3]]);
                            difSum += (float)Math.Abs(imgin_Y[pos[0], pos[5]] - imgin_Y[pos[0], pos[3]]);
                            //difSqRootAve = (float)Math.Sqrt(difSq) / 4;

                            //エントロピーに加算（ガウス分布をかける）
                            entropy += difSum / 4 * gaussary[x2 + filterRange, y2 + filterRange];

                            //リセット
                            difSum = 0f;
                        }
                    }
                    //imgoutに計算したentropyを代入
                    imgout[x1, y1] = entropy;
                    entropy = 0f;
                }
            }

            //配列を返す
            return imgout;

        }

        //（使ってない）
        //テスト用
        private void outIroIro()
        {
            //処理用の輝度のみ二次元配列
            float[,] process_Y = copy(imginput_Y);

            //ためしてみるガウスぼかし
            int[] gaussSize = new int[] { 0, 1, 3 };
            float[] gaussSigma = new float[] { 0.2f, 0.5f, 1.0f };

            //ためしてみる二値変換
            int[] blockSize = new int[] { 10, 15, 20 };
            float[] blockSigma = new float[] { 4.5f, 5.6f, 7.0f };
            float[] blockthreshold = new float[] { 8f, 11f, 14f };

            foreach(int gsize in gaussSize)
            {
                foreach(float gsigma in gaussSigma)
                {
                    foreach (int bsize in blockSize)
                    {
                        foreach (float bsigma in blockSigma)
                        {
                            foreach (float bthreshold in blockthreshold)
                            {
                                process_Y = copy(imginput_Y);
                                process_Y = gauss(process_Y, gsize, gsigma);
                                process_Y = binary_block(process_Y, bsize, bsigma, bthreshold);
                                outBmp(Y_To_Rgb(process_Y), "gSz_" + gsize + " gSg_" + gsigma + " bSz_" + bsize + " bSg_" + bsigma + " bTh_" + bthreshold + ".bmp");
                            }    
                        }
                    }    
                }
            }
            MessageBox.Show("かんりょー");
        }

        //（テスト用）
        private void button1_Click(object sender, EventArgs e)
        {


            List<int[]> list = new List<int[]>();
            list.Add(new int[] { 1, 2 });
            list.Add(new int[] { 3, 4 });
            list.Add(new int[] { 5, 6 });

            Console.WriteLine(list.Contains(new int[] { 1, 2 }));
            Console.WriteLine(list.Contains(new int[] { 2, 3 }));
            Console.WriteLine(list[0][0] + "" + list[0][1]);
            Console.WriteLine(list[1][0] + "" + list[1][1]);

            List<int> list2 = new List<int>();
            list2.Add(1);
            list2.Add(2);
            list2.Add(3);


            Console.WriteLine(list2.Count);
        }
    }
}