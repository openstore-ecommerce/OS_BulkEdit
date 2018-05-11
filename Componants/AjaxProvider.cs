using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Compilation;
using System.Xml;
using DotNetNuke.Common;
using DotNetNuke.Common.Utilities;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Services.Localization;
using NBrightCore.common;
using NBrightDNN;
using Nevoweb.DNN.NBrightBuy.Components;
using Nevoweb.DNN.NBrightBuy.Components.Products;
using Nevoweb.DNN.NBrightBuy.Components.Interfaces;
using RazorEngine.Compilation.ImpromptuInterface.InvokeExt;
using DotNetNuke.Entities.Users;

namespace OpenStore.Providers.OS_BulkEdit
{
    public class AjaxProvider : AjaxInterface
    {
        public override string Ajaxkey { get; set; }

        public override string ProcessCommand(string paramCmd, HttpContext context, string editlang = "")
        {
            if (!CheckRights())
            {
                return "Security Error.";
            }

            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var uilang = NBrightBuyUtils.SetContextLangauge(ajaxInfo); // Ajax breaks context with DNN, so reset the context language to match the client.
            var objCtrl = new NBrightBuyController();

            var strOut = "OS_BulkEdit Ajax Error";

            // NOTE: The paramCmd MUST start with the plugin ref. in lowercase. (links ajax provider to cmd)
            switch (paramCmd.ToLower())
            {
                case "os_bulkedit_test":
                    strOut = "<root>" + UserController.Instance.GetCurrentUserInfo().Username + "</root>";
                    break;
                case "os_bulkedit_getdata":
                    strOut = ProductAdminList(context);
                    break;
                case "os_bulkedit_deleterecord":
                    strOut = DeleteData(context);
                    break;
                case "os_bulkedit_saveitem":
                    DataSave(context);
                    break;
                case "os_bulkedit_selectchangedisable":
                    if (!NBrightBuyUtils.CheckRights()) break;
                    strOut = ProductFunctions.ProductDisable(context);
                    break;
                case "os_bulkedit_selectchangehidden":
                    if (!NBrightBuyUtils.CheckRights()) break;
                    strOut = ProductFunctions.ProductHidden(context);
                    break;
            }

            return strOut;

        }

        public override void Validate()
        {
        }

        #region "Methods"

        private String DeleteData(HttpContext context)
        {
            var objCtrl = new NBrightBuyController();

            //get uploaded params
            var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
            var itemid = ajaxInfo.GetXmlProperty("genxml/hidden/selecteditemid");
            if (Utils.IsNumeric(itemid))
            {
                // delete DB record
                objCtrl.Delete(Convert.ToInt32(itemid));

                NBrightBuyUtils.RemoveModCache(-2);
            }
            return ProductAdminList(context);
        }

        #endregion


        private Boolean CheckRights()
        {
            if (UserController.Instance.GetCurrentUserInfo().IsInRole(StoreSettings.ManagerRole) || UserController.Instance.GetCurrentUserInfo().IsInRole(StoreSettings.EditorRole) || UserController.Instance.GetCurrentUserInfo().IsInRole("Administrators"))
            {
                return true;
            }
            return false;
        }

        public string ProductAdminList(HttpContext context)
        {

            try
            {
                if (NBrightBuyUtils.CheckRights())
                {
                    var ajaxInfo = NBrightBuyUtils.GetAjaxInfo(context);

                    if (UserController.Instance.GetCurrentUserInfo().UserID <= 0) return null;

                    var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                    if (editlang == "") editlang = Utils.GetCurrentCulture();

                    NBrightBuyUtils.RemoveModCache(-2);

                    ajaxInfo.SetXmlProperty("genxml/hidden/razortemplate", "datalist.cshtml");

                    var strOut = "";

                    // select a specific entity data type for the product (used by plugins)
                    var entitytypecodelang = "PRDLANG";
                    var entitytypecode = "PRD";
                    
                    var filter = ajaxInfo.GetXmlProperty("genxml/hidden/filter");
                    var orderby = ajaxInfo.GetXmlProperty("genxml/hidden/orderby");
                    var returnLimit = ajaxInfo.GetXmlPropertyInt("genxml/hidden/returnlimit");
                    var pageNumber = ajaxInfo.GetXmlPropertyInt("genxml/hidden/pagenumber");
                    var pageSize = ajaxInfo.GetXmlPropertyInt("genxml/hidden/pagesize");
                    var cascade = ajaxInfo.GetXmlPropertyBool("genxml/hidden/cascade");
                    var portalId = PortalSettings.Current.PortalId;
                    if (ajaxInfo.GetXmlProperty("genxml/hidden/portalid") != "")
                    {
                        portalId = ajaxInfo.GetXmlPropertyInt("genxml/hidden/portalid");
                    }

                    var searchText = ajaxInfo.GetXmlProperty("genxml/hidden/searchtext");
                    var searchCategory = ajaxInfo.GetXmlProperty("genxml/hidden/searchcategory");

                    if (searchText != "") filter += " and (NB3.[ProductName] like '%" + searchText + "%' or NB3.[ProductRef] like '%" + searchText + "%' or NB3.[Summary] like '%" + searchText + "%' ) ";

                    if (Utils.IsNumeric(searchCategory))
                    {
                        if (orderby == "{bycategoryproduct}") orderby += searchCategory;
                        var objQual = DotNetNuke.Data.DataProvider.Instance().ObjectQualifier;
                        var dbOwner = DotNetNuke.Data.DataProvider.Instance().DatabaseOwner;
                        if (!cascade)
                            filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") ";
                        else
                            filter += " and NB1.[ItemId] in (select parentitemid from " + dbOwner + "[" + objQual + "NBrightBuy] where (typecode = 'CATXREF' and XrefItemId = " + searchCategory + ") or (typecode = 'CATCASCADE' and XrefItemId = " + searchCategory + ")) ";
                    }
                    else
                    {
                        if (orderby == "{bycategoryproduct}") orderby = " order by NB3.productname ";
                    }

                    // logic for client list of products
                    if (NBrightBuyUtils.IsClientOnly())
                    {
                        filter += " and NB1.ItemId in (select ParentItemId from dbo.[NBrightBuy] as NBclient where NBclient.TypeCode = 'USERPRDXREF' and NBclient.UserId = " + UserController.Instance.GetCurrentUserInfo().UserID.ToString("") + ") ";
                    }

                    var recordCount = 0;
                    var objCtrl = new NBrightBuyController();

                    if (pageNumber == 0) pageNumber = 1;
                    if (pageSize == 0) pageSize = 20;

                    // get only entity type required.  Do NOT use typecode, that is set by the filter.
                    recordCount = objCtrl.GetListCount(PortalSettings.Current.PortalId, -1, "PRD", filter, "", editlang);

                    // get selected entitytypecode.
                    var list = objCtrl.GetDataList(PortalSettings.Current.PortalId, -1, "PRD", "PRDLANG", editlang, filter, orderby, StoreSettings.Current.DebugMode, "", returnLimit, pageNumber, pageSize, recordCount);

                    return RenderProductAdminList(list, ajaxInfo, recordCount);

                }
            }
            catch (Exception ex)
            {
                Logging.LogException(ex);
                return ex.ToString();
            }
            return "";
        }

        public static String RenderProductAdminList(List<NBrightInfo> list, NBrightInfo ajaxInfo, int recordCount)
        {
            try
            {
                if (NBrightBuyUtils.CheckRights())
                {
                    if (list == null) return "";
                    if (UserController.Instance.GetCurrentUserInfo().UserID <= 0) return "";

                    var strOut = "";

                    // select a specific entity data type for the product (used by plugins)
                    var themeFolder = "config";
                    var pageNumber = ajaxInfo.GetXmlPropertyInt("genxml/hidden/pagenumber");
                    var pageSize = ajaxInfo.GetXmlPropertyInt("genxml/hidden/pagesize");
                    var razortemplate = ajaxInfo.GetXmlProperty("genxml/hidden/razortemplate");
                    var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                    if (editlang == "") editlang = Utils.GetCurrentCulture();

                    var templateControl = "/DesktopModules/NBright/OS_BulkEdit";

                    bool paging = pageSize > 0;

                    var passSettings = new Dictionary<string, string>();
                    foreach (var s in ajaxInfo.ToDictionary())
                    {
                        passSettings.Add(s.Key, s.Value);
                    }
                    foreach (var s in StoreSettings.Current.Settings()) // copy store setting, otherwise we get a byRef assignement
                    {
                        if (passSettings.ContainsKey(s.Key))
                            passSettings[s.Key] = s.Value;
                        else
                            passSettings.Add(s.Key, s.Value);
                    }

                    strOut = NBrightBuyUtils.RazorTemplRenderList(razortemplate, 0, "", list, templateControl, themeFolder, editlang, passSettings);

                    // add paging if needed
                    if (paging && (recordCount > pageSize))
                    {
                        var pg = new NBrightCore.controls.PagingCtrl();
                        strOut += pg.RenderPager(recordCount, pageSize, pageNumber);
                    }

                    return strOut;

                }
                return "";
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }

        }

        public void DataSave(HttpContext context)
        {
            if (NBrightBuyUtils.CheckRights())
            {
                var ajaxInfo = NBrightBuyUtils.GetAjaxFields(context);
                var editlang = ajaxInfo.GetXmlProperty("genxml/hidden/editlang");
                if (editlang == "") editlang = Utils.GetCurrentCulture();

                var modelXml = Utils.UnCode(ajaxInfo.GetXmlProperty("genxml/hidden/xmlupdatemodeldata"));
                var nbi = new NBrightInfo();
                nbi.XMLData = modelXml;
                var nodList = nbi.XMLDoc.SelectNodes("root/models");
                foreach (XmlNode xNod in nodList)
                {
                    var itemNod = xNod.SelectSingleNode("./@productid");
                    var itemid = 0;
                    if (itemNod != null && Utils.IsNumeric(itemNod.Value))
                    {
                        itemid = Convert.ToInt32(itemNod.Value);
                    }
                    if (itemid > 0)
                    {
                        var updateList = NBrightBuyUtils.GetGenXmlListByAjax(xNod.InnerXml, "", editlang);
                        var prdData = new ProductData(Convert.ToInt32(itemid), editlang, true, "PRD");
                        if (prdData.Exists)
                        {
                            //update models.
                            var lp = 1;
                            foreach (var upd in updateList)
                            {
                                prdData.DataLangRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/textbox/txtmodelname", upd.GetXmlProperty("genxml/textbox/txtmodelname"));
                                prdData.DataRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/textbox/txtmodelref", upd.GetXmlProperty("genxml/textbox/txtmodelref"));
                                prdData.DataRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/textbox/txtunitcost", upd.GetXmlPropertyDouble("genxml/textbox/txtunitcost").ToString(), System.TypeCode.Double);
                                prdData.DataRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/dropdownlist/taxrate", upd.GetXmlProperty("genxml/dropdownlist/taxrate"));
                                prdData.DataRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/textbox/weight", upd.GetXmlPropertyDouble("genxml/textbox/weight").ToString(), System.TypeCode.Double);
                                prdData.DataRecord.SetXmlProperty("genxml/models/genxml[" + lp + "]/textbox/txtqtyremaining", upd.GetXmlPropertyDouble("genxml/textbox/txtqtyremaining").ToString(), System.TypeCode.Double);
                                lp += 1;
                            }
                            prdData.Save(false, false);
                            // remove save GetData cache
                            var strCacheKey = prdData.Info.ItemID.ToString("") + "*" + prdData.DataRecord.TypeCode + "LANG*" + "*" + editlang;
                            Utils.RemoveCache(strCacheKey);
                        }
                    }
                }

                DataCache.ClearCache();
            }

        }



    }
}
