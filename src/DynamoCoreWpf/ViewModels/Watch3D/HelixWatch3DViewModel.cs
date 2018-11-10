﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Media.Media3D;
using System.Xml;
using Dynamo.Graph.Nodes;
using Dynamo.Graph.Workspaces;
using Dynamo.Logging;
using Dynamo.Selection;
using Dynamo.Wpf.Properties;
using Dynamo.Wpf.Rendering;
using Dynamo.Visualization;
using DynamoUtilities;
using HelixToolkit.Wpf.SharpDX;
using SharpDX;
using GeometryModel3D = HelixToolkit.Wpf.SharpDX.GeometryModel3D;
using Model3D = HelixToolkit.Wpf.SharpDX.Model3D;
using PerspectiveCamera = HelixToolkit.Wpf.SharpDX.PerspectiveCamera;
using TextInfo = HelixToolkit.Wpf.SharpDX.TextInfo;
using Newtonsoft.Json;

namespace Dynamo.Wpf.ViewModels.Watch3D
{
    public class CameraData
    { 
        // Default camera position data. These values have been rounded
        // to the nearest whole value.
        // eyeX="-16.9655136013663" eyeY="24.341577725171" eyeZ="50.6494323150915" 
        // lookX="12.4441040333119" lookY="-13.0110656299395" lookZ="-58.5449065206009" 
        // upX="-0.0812375695793365" upY="0.920504853452448" upZ="0.3821927158638" />

        private readonly Vector3D defaultCameraLookDirection = new Vector3D(12, -13, -58);
        private readonly Point3D defaultCameraPosition = new Point3D(-17, 24, 50);
        private readonly Vector3D defaultCameraUpDirection = new Vector3D(0, 1, 0);
        private const double defaultNearPlaneDistance = 0.1;
        private const double defaultFarPlaneDistance = 10000000;

        [JsonIgnore]
        public Point3D EyePosition { get { return new Point3D(EyeX, EyeY, EyeZ); } }
        [JsonIgnore]
        public Vector3D UpDirection { get { return new Vector3D(UpX, UpY, UpZ); } }
        [JsonIgnore]
        public Vector3D LookDirection { get { return new Vector3D(LookX, LookY, LookZ); } }
        [JsonIgnore]
        public double NearPlaneDistance { get; set; }
        [JsonIgnore]
        public double FarPlaneDistance { get; set; }

        // JSON camera data
        public string Name { get; set; }
        public double EyeX { get; set; }
        public double EyeY { get; set; }
        public double EyeZ { get; set; }
        public double LookX { get; set; }
        public double LookY { get; set; }
        public double LookZ { get; set; }
        public double UpX { get; set; }
        public double UpY { get; set; }
        public double UpZ { get; set; }

        public CameraData()
        {
            NearPlaneDistance = defaultNearPlaneDistance;
            FarPlaneDistance = defaultFarPlaneDistance;

            Name = "Default Camera";
            EyeX = defaultCameraPosition.X;
            EyeY = defaultCameraPosition.Y;
            EyeZ = defaultCameraPosition.Z;
            LookX = defaultCameraLookDirection.X;
            LookY = defaultCameraLookDirection.Y;
            LookZ = defaultCameraLookDirection.Z;
            UpX = defaultCameraUpDirection.X;
            UpY = defaultCameraUpDirection.Y;
            UpZ = defaultCameraUpDirection.Z;
        }
        
        public override bool Equals(object obj)
        {
            var other = obj as CameraData;
            return obj is CameraData && this.Name == other.Name
                   && Math.Abs(this.EyeX - other.EyeX) < 0.0001
                   && Math.Abs(this.EyeY - other.EyeY) < 0.0001
                   && Math.Abs(this.EyeZ - other.EyeZ) < 0.0001
                   && Math.Abs(this.LookX - other.LookX) < 0.0001
                   && Math.Abs(this.LookY - other.LookY) < 0.0001
                   && Math.Abs(this.LookZ - other.LookZ) < 0.0001
                   && Math.Abs(this.UpX - other.UpX) < 0.0001
                   && Math.Abs(this.UpY - other.UpY) < 0.0001
                   && Math.Abs(this.UpZ - other.UpZ) < 0.0001
                   && Math.Abs(this.NearPlaneDistance - other.NearPlaneDistance) < 0.0001
                   && Math.Abs(this.FarPlaneDistance - other.FarPlaneDistance) < 0.0001;
        }


    }

    /// <summary>
    /// The HelixWatch3DViewModel establishes a full rendering 
    /// context using the HelixToolkit. An instance of this class
    /// can act as the data source for a <see cref="Watch3DView"/>
    /// </summary>
    public class HelixWatch3DViewModel : DefaultWatch3DViewModel
    {
        #region private members
        private PerspectiveCamera camera;
        private const float defaultLabelOffset = 0.025f;

        private const string TextKey = ":text";
        private List<Model3D> sceneItems;
        private Dictionary<string, string> nodesSelected = new Dictionary<string, string>();


        #endregion

        #region events

        public Object Model3DDictionaryMutex = new object();
        private Dictionary<string, Model3D> model3DDictionary = new Dictionary<string, Model3D>();
        private readonly Dictionary<string, List<Tuple<string, Vector3>>> labelPlaces 
            = new Dictionary<string, List<Tuple<string, Vector3>>>();

        public event Action<Model3D> RequestAttachToScene;
        protected void OnRequestAttachToScene(Model3D model3D)
        {
            if (RequestAttachToScene != null)
            {
                RequestAttachToScene(model3D);
            }
        }

        /// <summary>
        /// An envent requesting to create geometries from render packages.
        /// </summary>
        public event Action<RenderPackageCache, bool> RequestCreateModels;
        private void OnRequestCreateModels(RenderPackageCache packages, bool forceAsyncCall = false)
        {
            if (RequestCreateModels != null)
            {
                RequestCreateModels(packages, forceAsyncCall);
            }
        }

        /// <summary>
        /// An event requesting to remove geometries generated by the node.
        /// </summary>
        public event Action<NodeModel> RequestRemoveModels;
        private void OnRequestRemoveModels(NodeModel node)
        {
            if (RequestRemoveModels != null)
            {
                RequestRemoveModels(node);
            }
        }

        /// <summary>
        /// An event requesting a zoom to fit operation around the provided bounds.
        /// </summary>


        #endregion

        #region properties


        internal Dictionary<string, Model3D> Model3DDictionary
        {
            get
            {
                lock (Model3DDictionaryMutex)
                {
                    return model3DDictionary;
                }
            }

            set
            {
                lock (Model3DDictionaryMutex)
                {
                    model3DDictionary = value;
                }
            }
        }

        public PerspectiveCamera Camera
        {
            get
            {
                return this.camera;
            }

            set
            {
                camera = value;
                RaisePropertyChanged("Camera");
            }
        }

        public bool IsResizable { get; protected set; }

        private void UpdateSceneItems()
        {
            if (Model3DDictionary == null)
            {
                sceneItems = new List<Model3D>();
                return;
            }

            var values = Model3DDictionary.Values.ToList();
            if (Camera != null)
            {
                values.Sort(new Model3DComparer(Camera.Position));
            }

            sceneItems = values;
        }

        public IEnumerable<Model3D> SceneItems
        {
            get
            {
                if (sceneItems == null)
                {
                    UpdateSceneItems();
                }

                return sceneItems;
            }
        }


        #endregion

        /// <summary>
        /// Attempt to create a HelixWatch3DViewModel. If one cannot be created,
        /// fall back to creating a DefaultWatch3DViewModel and log the exception.
        /// </summary>
        /// <param name="model">The NodeModel to associate with the returned view model.</param>
        /// <param name="parameters">A Watch3DViewModelStartupParams object.</param>
        /// <param name="logger">A logger to be used to log the exception.</param>
        /// <returns></returns>
        public static DefaultWatch3DViewModel TryCreateHelixWatch3DViewModel(NodeModel model, Watch3DViewModelStartupParams parameters, DynamoLogger logger)
        {
            try
            {
                var vm = new HelixWatch3DViewModel(model, parameters);
                return vm;
            }
            catch (Exception ex)
            {
                logger.Log(Resources.BackgroundPreviewCreationFailureMessage, LogLevel.Console);
                logger.Log(ex.Message, LogLevel.File);

                var vm = new DefaultWatch3DViewModel(model, parameters)
                {
                    Active = false,
                    CanBeActivated = false
                };
                return vm;
            }
        }

        protected HelixWatch3DViewModel(NodeModel model, Watch3DViewModelStartupParams parameters) 
        : base(model, parameters)
        {
            Name = Resources.BackgroundPreviewName;
            IsResizable = false;
            //RenderTechniquesManager = new DynamoRenderTechniquesManager();
            //EffectsManager = new DynamoEffectsManager(RenderTechniquesManager);

            //SetupScene();
            //InitializeHelix();
        }

        public void SerializeCamera(XmlElement camerasElement)
        {
            if (camera == null) return;

            try
            {
                var node = XmlHelper.AddNode(camerasElement, "Camera");
                XmlHelper.AddAttribute(node, "Name", Name);
                XmlHelper.AddAttribute(node, "eyeX", camera.Position.X.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "eyeY", camera.Position.Y.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "eyeZ", camera.Position.Z.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "lookX", camera.LookDirection.X.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "lookY", camera.LookDirection.Y.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "lookZ", camera.LookDirection.Z.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "upX", camera.UpDirection.X.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "upY", camera.UpDirection.Y.ToString(CultureInfo.InvariantCulture));
                XmlHelper.AddAttribute(node, "upZ", camera.UpDirection.Z.ToString(CultureInfo.InvariantCulture));
                camerasElement.AppendChild(node);

            }
            catch (Exception ex)
            {
                logger.LogError(Properties.Resources.CameraDataSaveError);
                logger.Log(ex);
            }
        }

        /// <summary>
        /// Create a CameraData object from an XmlNode representing the Camera's serialized
        /// position data.
        /// </summary>
        /// <param name="cameraNode">The XmlNode containing the camera position data.</param>
        /// <returns></returns>
        public CameraData DeserializeCamera(XmlNode cameraNode)
        {
            if (cameraNode.Attributes == null || cameraNode.Attributes.Count == 0)
            {
                return new CameraData();
            }

            try
            {
                var name = cameraNode.Attributes["Name"].Value;
                var ex = float.Parse(cameraNode.Attributes["eyeX"].Value, CultureInfo.InvariantCulture);
                var ey = float.Parse(cameraNode.Attributes["eyeY"].Value, CultureInfo.InvariantCulture);
                var ez = float.Parse(cameraNode.Attributes["eyeZ"].Value, CultureInfo.InvariantCulture);
                var lx = float.Parse(cameraNode.Attributes["lookX"].Value, CultureInfo.InvariantCulture);
                var ly = float.Parse(cameraNode.Attributes["lookY"].Value, CultureInfo.InvariantCulture);
                var lz = float.Parse(cameraNode.Attributes["lookZ"].Value, CultureInfo.InvariantCulture);
                var ux = float.Parse(cameraNode.Attributes["upX"].Value, CultureInfo.InvariantCulture);
                var uy = float.Parse(cameraNode.Attributes["upY"].Value, CultureInfo.InvariantCulture);
                var uz = float.Parse(cameraNode.Attributes["upZ"].Value, CultureInfo.InvariantCulture);

                var camData = new CameraData
                {
                    Name = name,
                    EyeX = ex,
                    EyeY = ey,
                    EyeZ = ez,
                    LookX = lx,
                    LookY = ly,
                    LookZ = lz,
                    UpX = ux,
                    UpY = uy,
                    UpZ = uz
                };

                return camData;
            }
            catch (Exception ex)
            {
                logger.LogError(Properties.Resources.CameraDataLoadError);
                logger.Log(ex);
            }

            return new CameraData();
        }

        public override void RemoveGeometryForNode(NodeModel node)
        {
            if (Active)
            {
                // Raise request for model objects to be deleted on the UI thread.
                OnRequestRemoveModels(node);
            }
        }

        public override void AddGeometryForRenderPackages(RenderPackageCache packages, bool forceAsyncCall = false)
        {
            if (Active)
            {
                // Raise request for model objects to be created on the UI thread.
                OnRequestCreateModels(packages, forceAsyncCall);
            }
        }

        protected override void OnWorkspaceOpening(object obj)
        {
            XmlDocument doc = obj as XmlDocument;
            if (doc != null)
            {
                var camerasElements = doc.GetElementsByTagName("Cameras");
                if (camerasElements.Count == 0)
                {
                    return;
                }

                foreach (XmlNode cameraNode in camerasElements[0].ChildNodes)
                {
                    try
                    {
                        var camData = DeserializeCamera(cameraNode);
                        SetCameraData(camData);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex.Message);
                        logger.Log(ex);
                    }
                }

                return;
            }

            ExtraWorkspaceViewInfo workspaceViewInfo = obj as ExtraWorkspaceViewInfo;
            if (workspaceViewInfo != null)
            {
                var cameraJson = workspaceViewInfo.Camera.ToString();

                var settings = new JsonSerializerSettings
                {
                    Error = (sender, args) =>
                    {
                        args.ErrorContext.Handled = true;
                        Console.WriteLine(args.ErrorContext.Error);
                    },
                    ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
                    TypeNameHandling = TypeNameHandling.Auto,
                    Formatting = Newtonsoft.Json.Formatting.Indented,
                    Culture = CultureInfo.InvariantCulture
                };

                var cameraData = JsonConvert.DeserializeObject<CameraData>(cameraJson, settings);
                SetCameraData(cameraData);
            }
        }

        protected override void OnWorkspaceSaving(XmlDocument doc)
        {
            var root = doc.DocumentElement;
            if (root == null)
            {
                return;
            }

            var camerasElement = doc.CreateElement("Cameras");
            SerializeCamera(camerasElement);
            root.AppendChild(camerasElement);
        }

        protected override void SelectionChangedHandler(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Reset:

                    // When deselecting (by clicking on the canvas), all the highlighted HelixWatch3D previews are switched off
                    // This results in a scenario whereby the list item in the WatchTree/PreviewBubble is still selected, and its
                    // labels are still displayed in the preview, but the highlighting has been switched off.
                    // In order to prevent this unintuitive UX behavior, the nodes will first be checked if they are in the 
                    // nodesSelected dictionary - if they are, they will not be switched off.
                    var geometryModels = new Dictionary<string, Model3D>();
                    foreach (var model in Model3DDictionary)
                    {
                        var nodePath = model.Key.Contains(':') ? model.Key.Remove(model.Key.IndexOf(':')) : model.Key;
                        if (model.Value is GeometryModel3D && !nodesSelected.ContainsKey(nodePath))
                        {
                            geometryModels.Add(model.Key, model.Value);
                        }
                    }

                    foreach (var geometryModel in geometryModels)
                    {
                        geometryModel.Value.SetValue(AttachedProperties.ShowSelectedProperty, false);
                    }
                    break;

                case NotifyCollectionChangedAction.Remove:
                    SetSelection(e.OldItems, false);
                    break;

                case NotifyCollectionChangedAction.Add:

                    // When a node is added to the workspace, it is also added
                    // to the selection. When running automatically, this addition
                    // also triggers an execution. This would successive calls to render.
                    // To prevent this, we maintain a collection of recently added nodes, and
                    // we check if the selection is an addition and if all of the recently
                    // added nodes are contained in that selection. if so, we skip the render
                    // as this render will occur after the upcoming execution.
                    if (e.Action == NotifyCollectionChangedAction.Add && recentlyAddedNodes.Any()
                        && recentlyAddedNodes.TrueForAll(n => e.NewItems.Contains((object)n)))
                    {
                        recentlyAddedNodes.Clear();
                        break;
                    }

                    SetSelection(e.NewItems, true);
                    break;
            }

            if (IsolationMode)
            {
                OnIsolationModeRequestUpdate();
            }
        }

        protected override void OnNodePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var node = sender as NodeModel;
            if (node == null)
            {
                return;
            }
            node.WasRenderPackageUpdatedAfterExecution = false;

            switch (e.PropertyName)
            {
                case "CachedValue":
                    Debug.WriteLine(string.Format("Requesting render packages for {0}", node.GUID));
                    node.RequestVisualUpdateAsync(scheduler, engineManager.EngineController, renderPackageFactory);
                    break;

                case "DisplayLabels":
                    node.RequestVisualUpdateAsync(scheduler, engineManager.EngineController, renderPackageFactory, true);
                    break;

                case "IsVisible":
                    var geoms = FindAllGeometryModel3DsForNode(node);
                    foreach(var g in geoms)
                    {
                        g.Value.Visibility = node.IsVisible ? Visibility.Visible : Visibility.Hidden;
                        //RaisePropertyChanged("SceneItems");
                    }

                    node.RequestVisualUpdateAsync(scheduler, engineManager.EngineController, renderPackageFactory, true);
                    break;

                //case "IsFrozen":
                //    HashSet<NodeModel> gathered = new HashSet<NodeModel>();
                //    node.GetDownstreamNodes(node, gathered);
                //    SetGeometryFrozen(gathered);
                //    break;
            }
        }

        /// <summary>
        /// Update the attached properties and recalculate transparency sorting
        /// after any update under Isolate Selected Geometry mode.
        /// </summary>
        protected override void OnIsolationModeRequestUpdate()
        {
            Model3DDictionary.Values.OfType<GeometryModel3D>().ToList().
                ForEach(g => AttachedProperties.SetIsolationMode(g, IsolationMode));
            OnSceneItemsChanged();
        }

        public override CameraData GetCameraInformation()
        {
            return camera.ToCameraData(Name);
        }

        #region private methods

        private void OnSceneItemsChanged()
        {
            UpdateSceneItems();
            RaisePropertyChanged("SceneItems");
            //OnRequestViewRefresh();
        }
   
        private KeyValuePair<string, Model3D>[] FindAllGeometryModel3DsForNode(NodeModel node)
        {
            KeyValuePair<string, Model3D>[] geometryModels;

            lock (Model3DDictionaryMutex)
            {
                geometryModels = Model3DDictionary
                        .Where(x => x.Key.Contains(node.AstIdentifierGuid) && x.Value is GeometryModel3D).ToArray();
            }

            return geometryModels;
        }

        private KeyValuePair<string, Model3D>[] FindAllGeometryModel3DsForNode(string identifier)
        {
            KeyValuePair<string, Model3D>[] geometryModels;

            lock (Model3DDictionaryMutex)
            {
                geometryModels = Model3DDictionary
                        .Where(x => x.Key.Contains(identifier) && x.Value is GeometryModel3D).ToArray();
            }

            return geometryModels;
        }


        private void SetSelection(IEnumerable items, bool isSelected)
        {
            foreach (var item in items)
            {
                var node = item as NodeModel;
                if (node == null)
                {
                    continue;
                }

                var geometryModels = FindAllGeometryModel3DsForNode(node);

                if (!geometryModels.Any())
                {
                    continue;
                }

                var modelValues = geometryModels.Select(x => x.Value);

                foreach(GeometryModel3D g in modelValues)
                {
                    g.SetValue(AttachedProperties.ShowSelectedProperty, isSelected);
                }
            }
        }

        public void SetCameraData(CameraData data)
        {
            if (Camera == null) return;

            Camera.LookDirection = data.LookDirection;
            Camera.Position = data.EyePosition;
            Camera.UpDirection = data.UpDirection;
            Camera.NearPlaneDistance = data.NearPlaneDistance;
            Camera.FarPlaneDistance = data.FarPlaneDistance;
        }

        /// <summary>
        /// Display a label for geometry based on the paths.
        /// </summary>
        public override void AddLabelForPath(string path)
        {
            // make var_guid from var_guid:0:1
            var nodePath = path.Contains(':') ? path.Remove(path.IndexOf(':')) : path;
            var labelName = nodePath + TextKey;
            lock (Model3DDictionaryMutex)
            {
                // first, remove current labels of the node
                // it does not crash if there is no such key in dictionary
                var sceneItemsChanged = Model3DDictionary.Remove(labelName);

                // it may be requested an array of items to put labels
                // for example, the first item in 2-dim array - path will look like var_guid:0
                // and it will select var_guid:0:0, var_guid:0:1, var_guid:0:2 and so on.
                // if there is a geometry to add label(s)
                List<Tuple<string, Vector3>> requestedLabelPlaces;
                if (labelPlaces.ContainsKey(nodePath) &&
                    (requestedLabelPlaces = labelPlaces[nodePath]
                        .Where(pair => pair.Item1 == path || pair.Item1.StartsWith(path + ":")).ToList()).Any())
                {
                    // If the nodesSelected Dictionary does not contain the current nodePath as a key,
                    // or if the current value of the nodePath key is not the same as the current path 
                    // (which is currently being selected) then, create new label(s) for the Watch3DView.
                    // Else, remove the label(s) in the Watch 3D View.

                    if (!nodesSelected.ContainsKey(nodePath) || nodesSelected[nodePath] != path)
                    {
                        // second, add requested labels
                        var textGeometry = HelixRenderPackage.InitText3D();
                        var bbText = new BillboardTextModel3D
                        {
                            Geometry = textGeometry
                        };

                        foreach (var id_position in requestedLabelPlaces)
                        {
                            var text = HelixRenderPackage.CleanTag(id_position.Item1);
                            var textPosition = id_position.Item2 + defaultLabelOffset;

                            var textInfo = new TextInfo(text, textPosition);
                            textGeometry.TextInfo.Add(textInfo);
                        }

                        if (nodesSelected.ContainsKey(nodePath))
                        {
                            ToggleTreeViewItemHighlighting(nodesSelected[nodePath], false); // switch off selection for previous path
                        }
                        
                        Model3DDictionary.Add(labelName, bbText);
                        sceneItemsChanged = true;
                        AttachAllGeometryModel3DToRenderHost();
                        nodesSelected[nodePath] = path;

                        ToggleTreeViewItemHighlighting(path, true); // switch on selection for current path
                    }

                    // if no node is being selected, that means the current node is being unselected
                    // and no node within the parent node is currently selected.
                    else
                    {
                        nodesSelected.Remove(nodePath);
                        ToggleTreeViewItemHighlighting(path, false);
                    }
                }

                if (sceneItemsChanged)
                {
                    OnSceneItemsChanged();
                }
            }
        }

        /// <summary>
        /// Remove the labels (in Watch3D View) for geometry once the Watch node is disconnected
        /// </summary>
        /// <param name="path"></param>
        public override void ClearPathLabel(string path)
        {
            var nodePath = path.Contains(':') ? path.Remove(path.IndexOf(':')) : path;
            var labelName = nodePath + TextKey;
            lock (Model3DDictionaryMutex)
            {
                var sceneItemsChanged = Model3DDictionary.Remove(labelName);
                if (sceneItemsChanged)
                {
                    OnSceneItemsChanged();
                }
            }
        }

        /// <summary>
        /// Toggles on the highlighting for the specific node (in Helix preview) when
        /// selected in the PreviewBubble as well as the Watch Node
        /// </summary>
        private void ToggleTreeViewItemHighlighting(string path, bool isSelected)
        {
            // First, deselect parentnode in DynamoSelection
            var nodePath = path.Contains(':') ? path.Remove(path.IndexOf(':')) : path;
            if (DynamoSelection.Instance.Selection.Any())
            {
                var selNodes = DynamoSelection.Instance.Selection.ToList().OfType<NodeModel>();
                foreach (var node in selNodes)
                {
                    if (node.AstIdentifierBase == nodePath)
                    {
                        node.Deselect();
                    }
                }
            }

            // Next, deselect the parentnode in HelixWatch3DView
            var nodeGeometryModels = Model3DDictionary.Where(x => x.Key.Contains(nodePath) && x.Value is GeometryModel3D).ToArray();
            foreach (var nodeGeometryModel in nodeGeometryModels)
            {
                nodeGeometryModel.Value.SetValue(AttachedProperties.ShowSelectedProperty, false);
            }

            // Then, select the individual node only if isSelected is true since all geometryModels' Selected Property is set to false
            if (isSelected)
            {
                var geometryModels = Model3DDictionary.Where(x => x.Key.StartsWith(path + ":") && x.Value is GeometryModel3D).ToArray();
                foreach (var geometryModel in geometryModels)
                {
                    geometryModel.Value.SetValue(AttachedProperties.ShowSelectedProperty, isSelected);
                }
            }
        }

        private void AttachAllGeometryModel3DToRenderHost()
        {
            foreach (var model3D in Model3DDictionary.Select(kvp => kvp.Value))
            {
                OnRequestAttachToScene(model3D);
            }
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //var effectsManager = EffectsManager as DynamoEffectsManager;
                //if (effectsManager != null) effectsManager.Dispose();

                foreach (var sceneItem in SceneItems)
                {
                    sceneItem.Dispose();
                }
            }
        }
    }

    /// <summary>
    /// The Model3DComparer is used to sort arrays of Model3D objects. 
    /// After sorting, the target array's objects will be organized
    /// as follows:
    /// 1. All not GeometryModel3D objects
    /// 2. All opaque mesh geometry
    /// 3. All opaque line geometry
    /// 4. All opaque point geometry
    /// 5. All transparent geometry, ordered by distance from the camera.
    /// 6. All text.
    /// </summary>
    public class Model3DComparer : IComparer<Model3D>
    {
        private readonly Vector3 cameraPosition;

        public Model3DComparer(Point3D cameraPosition)
        {
            this.cameraPosition = cameraPosition.ToVector3();
        }

        public int Compare(Model3D x, Model3D y)
        {
            var a = x as GeometryModel3D;
            var b = y as GeometryModel3D;

            // if at least one of them is not GeometryModel3D
            // we either sort by being GeometryModel3D type (result is 1 or -1) 
            // or don't care about order (result is 0)
            if (a == null && b == null)
            {
                return 0;
            }

            if (a == null)
            {
                return -1;
            }

            if (b == null)
            {
                return 1;
            }

            var textA = a.GetType() == typeof(BillboardTextModel3D);
            var textB = b.GetType() == typeof(BillboardTextModel3D);
            var result = textA.CompareTo(textB);

            // if at least one of them is text
            // we either sort by being text type (result is 1 or -1) 
            // or don't care about order (result is 0)
            if (textA || textB)
            {
                return result;
            }

            // under Isolate Selected Geometry mode, selected geometries will have higher precedence
            // and rendered as closer to the camera compared to unselected geometries
            var selectedA = AttachedProperties.GetIsolationMode(a) &&
                !AttachedProperties.GetShowSelected(a) && !AttachedProperties.IsSpecialRenderPackage(a);
            var selectedB = AttachedProperties.GetIsolationMode(b) &&
                !AttachedProperties.GetShowSelected(b) && !AttachedProperties.IsSpecialRenderPackage(b);
            result = selectedA.CompareTo(selectedB);
            if (result != 0) return result;

            // if only one of transA and transB has transparency, sort by having this property
            var transA = AttachedProperties.GetHasTransparencyProperty(a);
            var transB = AttachedProperties.GetHasTransparencyProperty(b);
            result = transA.CompareTo(transB);
            if (result != 0) return result;

            // if both items has transparency, sort by distance
            if (transA)
            {
                // compare distance
                var boundsA = a.Bounds;
                var boundsB = b.Bounds;
                var cpA = (boundsA.Maximum + boundsA.Minimum) / 2;
                var cpB = (boundsB.Maximum + boundsB.Minimum) / 2;
                var dA = Vector3.DistanceSquared(cpA, cameraPosition);
                var dB = Vector3.DistanceSquared(cpB, cameraPosition);
                result = -dA.CompareTo(dB);
                return result;
            }

            // if both items does not have transparency, sort following next order: mesh, line, point
            var pointA = a is PointGeometryModel3D;
            var pointB = b is PointGeometryModel3D;
            result = pointA.CompareTo(pointB);

            if (pointA || pointB)
            {
                return result;
            }

            var lineA = a is LineGeometryModel3D;
            var lineB = b is LineGeometryModel3D;
            return lineA.CompareTo(lineB);
        }
    }

    internal static class CameraExtensions
    {
        public static CameraData ToCameraData(this PerspectiveCamera camera, string name)
        {
            var camData = new CameraData
            {
                NearPlaneDistance = camera.NearPlaneDistance,
                FarPlaneDistance = camera.FarPlaneDistance,

                Name = name,
                EyeX = camera.Position.X,
                EyeY = camera.Position.Y,
                EyeZ = camera.Position.Z,
                LookX = camera.LookDirection.X,
                LookY = camera.LookDirection.Y,
                LookZ = camera.LookDirection.Z,
                UpX = camera.UpDirection.X,
                UpY = camera.UpDirection.Y,
                UpZ = camera.UpDirection.Z
            };

            return camData;
        }
    }

}
