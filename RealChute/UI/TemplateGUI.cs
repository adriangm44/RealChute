﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RealChute.Libraries.Materials;
using RealChute.Extensions;
using RealChute.Libraries.Textures;
using RealChute.Utils;
using UnityEngine;
using SelectorType = RealChute.ProceduralChute.SelectorType;

/* RealChute was made by Christophe Savard (stupid_chris). You are free to copy, fork, and modify RealChute as you see
 * fit. However, redistribution is only permitted for unmodified versions of RealChute, and under attribution clause.
 * If you want to distribute a modified version of RealChute, be it code, textures, configs, or any other asset and
 * piece of work, you must get my explicit permission on the matter through a private channel, and must also distribute
 * it through the attribution clause, and must make it clear to anyone using your modification of my work that they
 * must report any problem related to this usage to you, and not to me. This clause expires if I happen to be
 * inactive (no connection) for a period of 90 days on the official KSP forums. In that case, the license reverts
 * back to CC-BY-NC-SA 4.0 INTL.*/

namespace RealChute.UI
{
    /// <summary>
    /// Which mode parachute calculations are in
    /// </summary>
    public enum CalculationsMode
    {
        Automatic = 1,
        Manual = 2
    }

    public class TemplateGUI
    {
        #region Properties
        private ProceduralChute pChute
        {
            get { return this.template.pChute; }
        }

        private Parachute parachute
        {
            get { return this.template.parachute; }
        }

        private EditorGUI editorGUI
        {
            get { return this.template.editorGUI; }
        }

        private bool secondary
        {
            get { return this.template.secondary; }
        }

        private ParachuteMaterial material
        {
            get { return this.template.material; }
            set { this.template.material = value; }
        }

        private ModelConfig model
        {
            get { return this.template.model; }
        }

        private CelestialBody body
        {
            get { return this.template.body; }
        }

        public List<string> errors
        {
            get
            {
                List<string> errors = new List<string>();
                float f, max = (float)this.body.GetMaxAtmosphereAltitude();
                switch (this.mode)
                {
                    case CalculationsMode.Automatic:
                        {
                            if (!this.getMass && (!float.TryParse(this.mass, out f) || !GUIUtils.CheckRange(f, 0.1f, 10000))) { errors.Add("Craft mass"); }
                            switch (this.type)
                            {
                                case ParachuteType.Main:
                                    {
                                        if (!float.TryParse(this.landingSpeed, out f) || !GUIUtils.CheckRange(f, 0.1f, 300)) { errors.Add("Landing speed"); }
                                        break;
                                    }
                                case ParachuteType.Drogue:
                                    {
                                        if (!float.TryParse(this.landingSpeed, out f) && !GUIUtils.CheckRange(f, 0.1f, 5000)) { errors.Add("Landing speed"); }
                                        if ((!float.TryParse(this.refDepAlt, out f) || !GUIUtils.CheckRange(f, 10, max))) { errors.Add("Mains planned deployment alt"); }
                                        break;
                                    }
                                case ParachuteType.Drag:
                                    {
                                        if (!float.TryParse(this.landingSpeed, out f) || !GUIUtils.CheckRange(f, 0.1f, 300)) { errors.Add("Landing speed"); }
                                        if (!float.TryParse(this.deceleration, out f) || !GUIUtils.CheckRange(f, 0.1f, 100)) { errors.Add("Wanted deceleration"); }
                                        break;
                                    }
                            }
                            if (!float.TryParse(this.chuteCount, out f) || !GUIUtils.CheckRange(f, 1, 100)) { errors.Add("Parachute count"); }
                            break;
                        }

                    case CalculationsMode.Manual:
                        {
                            float p, d;
                            if (!float.TryParse(this.preDepDiam, out p)) { p = 0; }
                            if (!float.TryParse(this.depDiam, out d)) { d = 0; }
                            if (!GUIUtils.CheckRange(p, 0.5f, d)) { errors.Add("Predeployed diameter"); }
                            if (!GUIUtils.CheckRange(d, 1, this.pChute.textures == null ? 70 : this.model.maxDiam)) { errors.Add("Deployed diameter"); }
                            break;
                        }

                    default:
                        break;
                }

                if (!float.TryParse(this.predepClause, out f) || (this.isPressure ? !GUIUtils.CheckRange(f, 0.0001f, (float)body.GetPressureASL()) : !GUIUtils.CheckRange(f, 10, max)))
                {
                    errors.Add(this.isPressure ? "Predeployment pressure" : "Predeployment altitude");
                }
                if (!float.TryParse(this.deploymentAlt, out f) || !GUIUtils.CheckRange(f, 10, max)) { errors.Add("Deployment altitude"); }
                if (!GUIUtils.TryParseWithEmpty(this.cutAlt, out f) || !GUIUtils.CheckRange(f, -1, max)) { errors.Add("Autocut altitude"); }
                if (!float.TryParse(this.preDepSpeed, out f) || !GUIUtils.CheckRange(f, 0.5f, 5)) { errors.Add("Predeployment speed"); }
                if (!float.TryParse(this.depSpeed, out f) || !GUIUtils.CheckRange(f, 1, 10)) { errors.Add("Deployment speed"); }
                return errors;
            }
        }
        #endregion

        #region Fields
        private ChuteTemplate template = null;
        private GUISkin skins = HighLogic.Skin;
        internal Rect materialsWindow = new Rect(), drag = new Rect();
        internal int matId = Guid.NewGuid().GetHashCode();
        internal bool materialsVisible = false;
        internal Vector2 parachuteScroll = new Vector2(), materialsScroll = new Vector2();
        public int chuteID = -1, modelID = -1, materialsID = 0;
        public ParachuteType type = ParachuteType.Main, lastType = ParachuteType.Main;
        private string typeString = string.Empty, lastTypeString = string.Empty;
        public CalculationsMode mode = CalculationsMode.Automatic;
        public bool isPressure = false;
        public bool getMass = true, useDry = true;
        public string preDepDiam = string.Empty, depDiam = string.Empty, predepClause = string.Empty;
        public string mass = "10", landingSpeed = "6", deceleration = "10", refDepAlt = "700", chuteCount = "1";
        public string deploymentAlt = string.Empty, landingAlt = "0", cutAlt = string.Empty;
        public string preDepSpeed = string.Empty, depSpeed = string.Empty;
        #endregion

        #region Constructor
        /// <summary>
        /// Creates an empty Template GUI
        /// </summary>
        public TemplateGUI() { }

        /// <summary>
        /// Generates a new TemplateGUI from the given ChuteTemplate
        /// </summary>
        /// <param name="template"></param>
        public TemplateGUI(ChuteTemplate template)
        {
            this.template = template;
        }
        #endregion

        #region Methods
        //Type switchup
        internal void SwitchType()
        {
            if (this.lastType != this.type)
            {
                switch (this.type)
                {
                    case ParachuteType.Main:
                        {
                            this.landingSpeed = "6";
                            this.deploymentAlt = "700";
                            this.predepClause = this.isPressure ? "0.01" : "25000";
                            this.preDepSpeed = "2";
                            this.depSpeed = "6";
                            break;
                        }

                    case ParachuteType.Drogue:
                        {
                            this.landingSpeed = "80";
                            this.deploymentAlt = "2500";
                            this.predepClause = this.isPressure ? "0.007" : "30000";
                            this.preDepSpeed = "1";
                            this.depSpeed = "3";
                            break;
                        }

                    case ParachuteType.Drag:
                        {
                            this.landingSpeed = "100";
                            this.deploymentAlt = "10";
                            this.predepClause = this.isPressure ? "0.5" : "50";
                            this.preDepSpeed = "1";
                            this.depSpeed = "2";
                            break;
                        }
                    default:
                        break;
                }
                this.lastType = this.type;
            }
        }

        //Texture selector GUI code
        internal void TextureSelector()
        {
            string[] cases = this.pChute.TextureEntries(SelectorType.CASE), chutes = this.pChute.TextureEntries(SelectorType.CHUTE), models = this.pChute.TextureEntries(SelectorType.MODEL);
            bool a = false, b = false, c = false;
            int h = 0;
            if (!this.secondary && cases.Length > 1) { h++; a = true; }
            if (chutes.Length > 1) { h++; b = true; }
            if (models.Length > 1) { h++; c = true; }
            if (h == 0) { return; }

            GUILayout.BeginHorizontal();

            #region Labels
            GUILayout.BeginVertical(GUILayout.Height(35 * h));

            //Labels
            if (a)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Case texture:", skins.label);
            }
            if (b)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Chute texture:", skins.label);
            }
            if (c)
            {
                GUILayout.FlexibleSpace();
                GUILayout.Label("Chute model: ", skins.label);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            #endregion

            #region Selectors
            GUILayout.BeginVertical(skins.box, GUILayout.Height(35 * h));
            //Boxes
            if (a)
            {
                GUILayout.FlexibleSpace();
                this.pChute.caseID = GUILayout.SelectionGrid(this.pChute.caseID, cases, cases.Length, this.skins.button);
            }

            if (b)
            {
                GUILayout.FlexibleSpace();
                this.chuteID = GUILayout.SelectionGrid(this.chuteID, chutes, chutes.Length, this.skins.button);
            }

            if (c)
            {
                GUILayout.FlexibleSpace();
                this.modelID = GUILayout.SelectionGrid(this.modelID, models, models.Length, this.skins.button);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndVertical();
            #endregion

            GUILayout.EndHorizontal();
        }

        //Materials selector GUI code
        internal void MaterialsSelector()
        {
            if (this.pChute.materials.count > 1)
            {
                GUILayout.BeginHorizontal(GUILayout.Height(20));
                GUILayout.BeginVertical();
                GUILayout.FlexibleSpace();
                GUILayout.Label("Current material: " + this.material.name, this.skins.label);
                GUILayout.EndVertical();
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Change material", this.skins.button, GUILayout.Width(150)))
                {
                    this.materialsVisible = !this.materialsVisible;
                }
                GUILayout.EndHorizontal();
            }
        }

        //Calculations GUI core
        internal void Calculations()
        {
            #region Calculations
            //Selection mode
            GUILayout.BeginHorizontal();
            GUILayout.Label("Calculations mode:", this.skins.label);
            this.mode = EnumUtils.SelectionGrid(this.mode, 2, this.skins.button);
            GUILayout.EndHorizontal();
            GUILayout.Space(5);

            //Calculations
            this.parachuteScroll = GUILayout.BeginScrollView(this.parachuteScroll, false, false, this.skins.horizontalScrollbar, this.skins.verticalScrollbar, this.skins.box, GUILayout.Height(160));

            string label;
            float max, min;
            switch (this.mode)
            {
                #region Automatic
                case CalculationsMode.Automatic:
                    {
                        this.type = EnumUtils.SelectionGrid(this.type, 3, this.skins.button);
                        GUILayout.BeginHorizontal();
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Toggle(this.getMass, "Use current craft mass", this.skins.button, GUILayout.Width(150))) { this.getMass = true; }
                        GUILayout.FlexibleSpace();
                        if (GUILayout.Toggle(!this.getMass, "Input craft mass", this.skins.button, GUILayout.Width(150))) { this.getMass = false; }
                        GUILayout.FlexibleSpace();
                        GUILayout.EndHorizontal();

                        if (this.getMass)
                        {
                            GUILayout.Label("Currently using " + (this.useDry ? "dry mass" : "wet mass"), skins.label);
                            if (GUILayout.Button("Switch to " + (this.useDry ? "wet mass" : "dry mass"), this.skins.button, GUILayout.Width(125))) { this.useDry = !this.useDry; }
                        }

                        else
                        {
                            GUIUtils.CreateEntryArea("Mass to use (t):", ref mass, 0.1f, 10000, 100);
                        }
                        max = 300;
                        switch (this.type)
                        {
                            case ParachuteType.Main:
                                label = "Wanted touchdown speed (m/s):"; break;
                            case ParachuteType.Drogue:
                                label = "Wanted speed at target alt (m/s):"; max = 5000; break;
                            case ParachuteType.Drag:
                                label = "Planned landing speed (m/s):"; break;
                            default:
                                label = string.Empty; break;
                        }
                        GUIUtils.CreateEntryArea(label, ref this.landingSpeed, 0.1f, max, 100);

                        if (this.type == ParachuteType.Drogue)
                        {
                            GUIUtils.CreateEntryArea("Target altitude (m):", ref this.refDepAlt, 10, (float)body.GetMaxAtmosphereAltitude(), 100);
                        }

                        if (this.type == ParachuteType.Drag)
                        {
                            GUIUtils.CreateEntryArea("Wanted deceleration (m/s²):", ref this.deceleration, 0.1f, 100, 100);
                        }

                        GUIUtils.CreateEntryArea("Parachutes used (parachutes):", ref this.chuteCount, 1, 100, 100);
                        break;
                    }
                #endregion

                #region Manual
                case CalculationsMode.Manual:
                    {
                        float p, d;
                        if (!float.TryParse(this.preDepDiam, out p)) { p = -1; }
                        if (!float.TryParse(this.depDiam, out d)) { d = -1; }

                        //Predeployed diameter
                        GUIUtils.CreateEntryArea("Predeployed diameter (m):", ref this.preDepDiam, 0.5f, d, 100);
                        if (p != -1) { GUILayout.Label("Resulting area: " + RCUtils.GetArea(p).ToString("0.00") + "m²", this.skins.label); }
                        else { GUILayout.Label("Resulting predeployed area: --- m²", this.skins.label); }

                        //Deployed diameter
                        GUIUtils.CreateEntryArea("Deployed diameter (m):", ref this.depDiam, 1, (this.pChute.textures == null ? 70 : this.model.maxDiam), 100);
                        if (d != 1) { GUILayout.Label("Resulting area: " + RCUtils.GetArea(d).ToString("0.00") + "m²", this.skins.label); }
                        else { GUILayout.Label("Resulting deployed area: --- m²", this.skins.label); }
                        break;
                    }
                #endregion

                default:
                    break;
            }

            GUILayout.EndScrollView();
            #endregion

            #region Specific
            //Pressure/alt toggle
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            if (GUILayout.Toggle(this.isPressure, "Pressure predeployment", this.skins.toggle))
            {
                if (!this.isPressure)
                {
                    this.isPressure = true;
                    this.predepClause = this.parachute.minPressure.ToString();
                }
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Toggle(!this.isPressure, "Altitude predeployment", this.skins.toggle))
            {
                if (this.isPressure)
                {
                    this.isPressure = false;
                    this.predepClause = this.parachute.minDeployment.ToString();
                }
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            //Pressure/alt selection
            if (this.isPressure)
            {
                label = "Predeployment pressure (atm):";
                min = 0.0001f;
                max = (float)this.body.GetPressureASL();
            }
            else
            {
                label = "Predeployment altitude (m):";
                min = 10;
                max = (float)body.GetMaxAtmosphereAltitude();
            }
            GUIUtils.CreateEntryArea(label, ref this.predepClause, min, max);

            //Deployment altitude
            GUIUtils.CreateEntryArea("Deployment altitude", ref deploymentAlt, 10, (float)body.GetMaxAtmosphereAltitude());

            //Cut altitude
            GUIUtils.CreateEmptyEntryArea("Autocut altitude (m):", ref this.cutAlt, -1, (float)this.body.GetMaxAtmosphereAltitude());

            //Predeployment speed
            GUIUtils.CreateEntryArea("Pre deployment speed (s):", ref preDepSpeed, 0.5f, 5);

            //Deployment speed
            GUIUtils.CreateEntryArea("Deployment speed (s):", ref depSpeed, 1, 10);
            #endregion
        }

        //Materials window GUI code
        internal void MaterialsWindow(int id)
        {
            UnityEngine.GUI.DragWindow(drag);
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            this.materialsScroll = GUILayout.BeginScrollView(this.materialsScroll, false, false, this.skins.horizontalScrollbar, this.skins.verticalScrollbar, this.skins.box, GUILayout.MaxHeight(200), GUILayout.Width(140));
            this.materialsID = GUILayout.SelectionGrid(this.materialsID, this.pChute.materials.materialNames, 1, this.skins.button);
            GUILayout.EndScrollView();
            GUILayout.BeginVertical();
            ParachuteMaterial material = this.pChute.materials.GetMaterial(this.materialsID);
            StringBuilder builder = new StringBuilder();
            builder.Append("Description:  ").AppendLine(material.description);
            builder.Append("\nDrag coefficient:  ").AppendLine(material.dragCoefficient.ToString("0.00#"));
            builder.Append("\nArea density:  ").Append(material.areaDensity * 1000).AppendLine("kg/m²\n");
            builder.Append("Area cost:  ").Append(material.areaCost.ToString()).Append("F/m²");
            GUILayout.Label(builder.ToString(), this.skins.label);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Choose material", this.skins.button, GUILayout.Width(150)))
            {
                this.material = material;
                this.materialsVisible = false;
            }
            if (GUILayout.Button("Cancel", this.skins.button, GUILayout.Width(150)))
            {
                this.materialsVisible = false;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
        #endregion
    }
}