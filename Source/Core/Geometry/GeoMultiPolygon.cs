﻿using DEETU.Tool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DEETU.Geometry
{
    [Serializable]
    public class GeoMultiPolygon : GeoGeometry
    {
        #region 字段
        private GeoParts _Parts;
        private double _MinX = double.MaxValue, _MinY = double.MaxValue, _MaxX = double.MinValue, _MaxY = double.MaxValue;
        #endregion

        #region 构造函数
        public GeoMultiPolygon()
        {
            _Parts = new GeoParts();
        }
        public GeoMultiPolygon(GeoPoints points)
        {
            _Parts = new GeoParts();
            _Parts.Add(points);
        }

        public GeoMultiPolygon(GeoParts parts)
        {
            _Parts = parts;
        }
        #endregion

        #region 属性

        public override GeoGeometryTypeConstant Type { get => GeoGeometryTypeConstant.MultiPolygon; }
        /// <summary>
        /// 获取或设置部分的集合
        /// </summary>
        public GeoParts Parts
        {
            get { return _Parts; }
            set { _Parts = value; }
        }

        /// <summary>
        /// 获取最小X
        /// </summary>
        public double MinX
        {
            get { return _MinX; }
        }
        /// <summary>
        /// 获取最小Y
        /// </summary>
        public double MinY
        {
            get { return _MinY; }
        }
        /// <summary>
        /// 获取最大X
        /// </summary>
        public double MaxX
        {
            get { return _MaxX; }
        }
        /// <summary>
        /// 获取最大Y
        /// </summary>
        public double MaxY
        {
            get { return _MaxY; }
        }

        #endregion

        #region 方法
        /// <summary>
        /// 获取外包矩形
        /// </summary>
        /// <returns></returns>
        public GeoRectangle GetEnvelope()
        {
            return new GeoRectangle(_MinX, _MaxX, _MinY, _MaxY);
        }
        /// <summary>
        /// 更新坐标范围
        /// </summary>
        public override void UpdateExtent()
        {
            CalExtent();
        }
        /// <summary>
        /// 复制
        /// </summary>
        /// <returns></returns>
        public override GeoGeometry Clone()
        {
            GeoMultiPolygon sMultiPolygon = new GeoMultiPolygon();
            sMultiPolygon.Parts = _Parts.Clone() as GeoParts;
            sMultiPolygon._MinX = _MinX;
            sMultiPolygon._MaxX = _MaxX;
            sMultiPolygon._MinY = _MinY;
            sMultiPolygon._MaxY = _MaxY;
            return sMultiPolygon;
        }
        #endregion

        #region 私有函数
        private void CalExtent()
        {
            _Parts.CalExtent();
            _MinX = _Parts.MinX;
            _MaxX = _Parts.MaxX;
            _MinY = _Parts.MinY;
            _MaxY = _Parts.MaxY;
        }
        #endregion
    }
}
